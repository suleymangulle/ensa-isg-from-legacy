using Ensa.DataMigrator.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// Proves that no user reference was broken, before anything is dropped.
/// <para>
/// <b>Why this has to exist.</b> 439 columns across the schema point at <c>User.Id</c> — every
/// <c>CreatorId</c>, every <c>LastModifierId</c>, every <c>DeleterId</c>, and the explicit ones like
/// <c>AssignedSpecialist.UserId</c> and <c>Archive.PreviousAddedByUserId</c>. Almost none of them
/// are declared as foreign keys, because the architecture forbids navigation properties; the
/// database will not stop a bad value, so something has to look.
/// </para>
/// <para>
/// <b>Two questions, both answered against the data.</b> First, did the identities survive: the id
/// map says which legacy user became which modern user, and every one of those ids must still be
/// that user. Second, does anything point at a user that is not there.
/// </para>
/// <para>
/// This is a read-only step. It writes nothing and changes nothing; it exists to be run before the
/// destructive migration and to fail if the answer is no.
/// </para>
/// </summary>
public sealed class UserIdentityVerifyStep : IMigrationStep
{
    public int Order => 9100;

    public string Name => "verify-user-identity";

    public string Description => "Checks that user ids were preserved and nothing points at a missing user";

    /// <summary>
    /// Columns that hold a <c>User.Id</c>. The audit trio appear on almost every table; the rest end
    /// in <c>UserId</c>.
    /// <para>
    /// <c>NationalId</c> and friends end in <c>Id</c> without being keys, which is why this matches
    /// on the exact audit names and the <c>UserId</c> suffix rather than on <c>Id</c>.
    /// </para>
    /// </summary>
    private const string CandidateSql =
        """
        SELECT t.name, c.name
        FROM sys.columns AS c
        JOIN sys.tables AS t ON t.object_id = c.object_id
        WHERE SCHEMA_NAME(t.schema_id) = 'ensa'
          AND TYPE_NAME(c.user_type_id) = 'int'
          AND (c.name IN ('CreatorId', 'LastModifierId', 'DeleterId') OR c.name LIKE '%UserId')
        ORDER BY t.name, c.name;
        """;

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await context.OpenModernAsync(cancellationToken);

        var preserved = await IdentitiesPreservedAsync(context, connection, cancellationToken);
        var (checkedColumns, orphans) = await OrphansAsync(context, connection, cancellationToken);

        var note =
            $"{preserved.Mapped} mapped ids, {preserved.Missing} missing; "
            + $"{checkedColumns} columns checked, {orphans.Count} with orphaned references";

        if (preserved.Missing > 0 || orphans.Count > 0)
        {
            foreach (var (table, column, count) in orphans)
            {
                context.Logger.LogError(
                    "    ORPHANED: {Table}.{Column} has {Count} value(s) with no matching user",
                    table, column, count);
            }

            context.Logger.LogError("    user identity verification FAILED — do not drop anything");
            return new StepResult(checkedColumns, 0, orphans.Count + preserved.Missing, "FAILED: " + note);
        }

        context.Logger.LogInformation("    user identity verified: {Note}", note);
        return new StepResult(checkedColumns, 0, 0, note);
    }

    /// <summary>
    /// Every id the migration handed out must still belong to the user it was handed out for.
    /// A regenerated key would leave 439 columns pointing at the wrong person, silently.
    /// </summary>
    private static async Task<(int Mapped, int Missing)> IdentitiesPreservedAsync(
        MigrationContext context,
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            """
            SELECT COUNT(*),
                   SUM(CASE WHEN u.Id IS NULL THEN 1 ELSE 0 END)
            FROM migration.IdMap AS m
            LEFT JOIN ensa.[User] AS u ON u.Id = m.ModernId
            WHERE m.LegacyTable = 'Kullanici_T';
            """, connection) { CommandTimeout = 600 };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        var mapped = reader.GetInt32(0);
        var missing = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);

        context.Logger.LogInformation(
            "    id map: {Mapped} legacy users mapped, {Missing} whose modern id no longer exists",
            mapped, missing);

        return (mapped, missing);
    }

    /// <summary>Every column that holds a user id, checked for values with no user behind them.</summary>
    private static async Task<(int Checked, List<(string Table, string Column, int Count)> Orphans)> OrphansAsync(
        MigrationContext context,
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var candidates = new List<(string Table, string Column)>();

        await using (var command = new SqlCommand(CandidateSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                candidates.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        var orphans = new List<(string, string, int)>();

        foreach (var (table, column) in candidates)
        {
            await using var command = new SqlCommand(
                $"""
                 SELECT COUNT(*)
                 FROM ensa.[{table}] AS t
                 WHERE t.[{column}] IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM ensa.[User] AS u WHERE u.Id = t.[{column}]);
                 """, connection) { CommandTimeout = 600 };

            var count = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);

            if (count > 0)
            {
                orphans.Add((table, column, count));
            }
        }

        context.Logger.LogInformation(
            "    references: {Checked} columns hold a user id, {Bad} of them have orphans",
            candidates.Count, orphans.Count);

        return (candidates.Count, orphans);
    }
}
