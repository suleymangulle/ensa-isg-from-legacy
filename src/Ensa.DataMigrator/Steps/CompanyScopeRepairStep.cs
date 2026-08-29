using Ensa.DataMigrator.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// Repairs <c>UserProfile.CompanyId</c> rows that an earlier version of <see cref="UserSplitStep"/>
/// wrote for people who are not customers.
///
/// <para>
/// <b>What went wrong.</b> That step copied legacy <c>Kullanici_T.FirmaId</c> onto every profile
/// that had one. <c>FirmaId</c> was not a customer binding: 731 of 766 legacy <c>Admin</c> accounts
/// carried one and 728 of those pointed at the organization's own company record. Because
/// <c>CompanyId</c> is the key of a query filter that fails closed, each of those people ended up
/// able to see exactly one company — their own OSGB — instead of every workplace their organization
/// serves.
/// </para>
///
/// <para>
/// <b>Why a repair rather than a re-run.</b> The corrected <see cref="UserSplitStep"/> only ever
/// writes a profile whose <c>CompanyId</c> is still null, which is right for a migration — a step
/// that widened an existing scope could undo a deliberate correction. It therefore cannot clear what
/// the defective version already wrote. Re-importing is not an option either: the accounts, the
/// companies and the offices are real rows that other data points at, and recreating them would
/// change ids the whole database references.
/// </para>
///
/// <para>
/// <b>What it will not do.</b> It writes one column, on rows it has proved belong to non-customers,
/// and nothing else. It never sets a <c>CompanyId</c>, never touches tenants, roles, user types,
/// companies, offices or <c>UserOffice</c>, and never deletes anything. A profile it cannot trace
/// back to a legacy account through <see cref="IdMap"/> is left exactly as it is: an account created
/// after the migration has no legacy row to classify it, and guessing is how a correction becomes a
/// second defect.
/// </para>
///
/// <para>
/// <b>How it is guarded.</b> The tool's own <c>--confirm &lt;database&gt;</c> interlock already
/// refuses to run against a destination the caller did not name out loud. On top of that this step
/// writes nothing unless <c>--repair-company-scope</c> is passed, so a full migration run reports
/// the finding and moves on. The update itself runs inside one transaction whose postconditions are
/// checked before it commits, and it rolls back if any of them fail.
/// </para>
///
/// <para>
/// <b>Idempotent.</b> The affected set is defined by the state it removes, so a second run finds
/// nothing to do and reports zero.
/// </para>
/// </summary>
public sealed class CompanyScopeRepairStep : IMigrationStep
{
    /// <summary>Runs last: it corrects what the earlier steps produced.</summary>
    public int Order => 99;

    public string Name => "company-scope-repair";

    public string Description =>
        "Clears UserProfile.CompanyId for migrated users the legacy data proves are not customers";

    /// <summary>
    /// Whether to actually write. Off by default so that reporting is the safe path and mutating is
    /// the deliberate one — the same shape <c>LogStep.IncludeApplicationLog</c> uses.
    /// </summary>
    public bool Apply { get; init; }

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var users = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        if (users.Count == 0)
        {
            return new StepResult(0, 0, 0, "company-scope repair: no user id map, nothing to classify");
        }

        // Which legacy accounts are customers. Read from the legacy database, which stays read-only
        // throughout: this is the classification the repair is allowed to act on and the only one.
        var customers = new HashSet<int>();
        var legacyRead = 0;

        await using (var legacy = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT KullaniciId, PersonelTuru FROM Kullanici_T WHERE PersonelTuru IS NOT NULL", legacy))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                legacyRead++;

                // The same classifier the corrected migration step writes with, so the two can
                // never disagree about who a customer is.
                if (LegacyStaffType.IsCustomer(reader.GetString(1)))
                {
                    customers.Add(reader.GetInt32(0));
                }
            }
        }

        // Every profile that currently carries a company, and whether the legacy account behind it
        // says it should.
        var scoped = await ReadScopedProfilesAsync(context, cancellationToken);

        var traceable = new Dictionary<int, int>(users.Count);
        foreach (var (legacyId, modernId) in users)
        {
            traceable[modernId] = legacyId;
        }

        var repairable = new List<int>();
        var keptCustomers = 0;
        var keptUntraceable = 0;

        foreach (var userId in scoped)
        {
            if (!traceable.TryGetValue(userId, out var legacyId))
            {
                keptUntraceable++;
                continue;
            }

            if (customers.Contains(legacyId))
            {
                keptCustomers++;
                continue;
            }

            repairable.Add(userId);
        }

        context.Logger.LogInformation(
            "    company scope: {Scoped} scoped profile(s); {Repairable} not customers, "
            + "{Customers} genuine customers, {Untraceable} created after the migration",
            scoped.Count, repairable.Count, keptCustomers, keptUntraceable);

        var summary =
            $"company-scope repair: {scoped.Count} scoped, {repairable.Count} to clear, "
            + $"{keptCustomers} customers kept, {keptUntraceable} untraceable kept";

        if (context.DryRun || !Apply)
        {
            var why = context.DryRun ? "dry run" : "pass --repair-company-scope to apply";
            context.Logger.LogInformation("    company scope: reporting only ({Why})", why);
            return new StepResult(legacyRead, 0, repairable.Count, $"{summary}; not applied ({why})");
        }

        if (repairable.Count == 0)
        {
            return new StepResult(legacyRead, 0, 0, $"{summary}; nothing to do");
        }

        var cleared = await ApplyAsync(context, repairable, keptCustomers + keptUntraceable, cancellationToken);

        return new StepResult(legacyRead, cleared, keptCustomers + keptUntraceable, $"{summary}; cleared {cleared}");
    }

    /// <summary>The user ids of every profile that currently carries a company.</summary>
    private static async Task<List<int>> ReadScopedProfilesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var scoped = new List<int>();

        await using var connection = await context.OpenModernAsync(cancellationToken);
        await using var command = new SqlCommand(
            "SELECT UserId FROM ensa.UserProfile WHERE CompanyId IS NOT NULL ORDER BY UserId",
            connection) { CommandTimeout = 600 };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            scoped.Add(reader.GetInt32(0));
        }

        return scoped;
    }

    /// <summary>
    /// Clears the column for the named users, inside one transaction, and proves the outcome before
    /// letting it stand.
    /// <para>
    /// Two postconditions, both checked against the database rather than against what this method
    /// believes it did: exactly the expected number of profiles still carries a company, and none of
    /// the ids it was told to clear still does. Either one failing rolls the whole thing back.
    /// </para>
    /// </summary>
    private static async Task<int> ApplyAsync(
        MigrationContext context,
        List<int> repairable,
        int expectedRemaining,
        CancellationToken cancellationToken)
    {
        await using var connection = await context.OpenModernAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var cleared = 0;

            foreach (var chunk in repairable.Chunk(500))
            {
                var parameters = string.Join(",", chunk.Select((_, i) => $"@u{i}"));

                await using var command = new SqlCommand(
                    $"UPDATE ensa.UserProfile SET CompanyId = NULL "
                    + $"WHERE CompanyId IS NOT NULL AND UserId IN ({parameters});",
                    connection,
                    transaction) { CommandTimeout = 1800 };

                for (var i = 0; i < chunk.Length; i++)
                {
                    command.Parameters.AddWithValue($"@u{i}", chunk[i]);
                }

                cleared += await command.ExecuteNonQueryAsync(cancellationToken);
            }

            var remaining = await ScalarAsync(
                connection,
                transaction,
                "SELECT COUNT(*) FROM ensa.UserProfile WHERE CompanyId IS NOT NULL",
                cancellationToken);

            if (remaining != expectedRemaining)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Company-scope repair rolled back: {remaining} profile(s) still carry a company, "
                    + $"expected {expectedRemaining}.");
            }

            var stillScoped = 0;

            foreach (var chunk in repairable.Chunk(500))
            {
                var parameters = string.Join(",", chunk.Select((_, i) => $"@u{i}"));

                await using var command = new SqlCommand(
                    $"SELECT COUNT(*) FROM ensa.UserProfile "
                    + $"WHERE CompanyId IS NOT NULL AND UserId IN ({parameters});",
                    connection,
                    transaction) { CommandTimeout = 600 };

                for (var i = 0; i < chunk.Length; i++)
                {
                    command.Parameters.AddWithValue($"@u{i}", chunk[i]);
                }

                stillScoped += (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
            }

            if (stillScoped != 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Company-scope repair rolled back: {stillScoped} of the targeted profile(s) "
                    + "still carry a company.");
            }

            await transaction.CommitAsync(cancellationToken);

            context.Logger.LogInformation(
                "    company scope: cleared {Cleared} profile(s); {Remaining} genuine customer "
                + "scope(s) left untouched",
                cleared, remaining);

            return cleared;
        }
        catch
        {
            // A rollback that has already happened throws here; the original failure is what matters.
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // already rolled back
            }

            throw;
        }
    }

    private static async Task<int> ScalarAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection, transaction) { CommandTimeout = 600 };
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }
}
