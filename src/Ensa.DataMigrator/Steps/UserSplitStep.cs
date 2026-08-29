using Ensa.DataMigrator.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// Moves everything that is not an account out of the <c>User</c> table.
/// <para>
/// <b>What it is fixing.</b> <c>User</c> had grown to 49 columns — fifteen belonging to
/// <c>IdentityUser</c> and the rest belonging to Ensa: an address, a salary, a photograph, another
/// system's password. The identity contract allows exactly one application-specific property on the
/// Identity user, <c>TenantId</c>, so the rest moves into tables that were built for it.
/// </para>
/// <para>
/// <b>Two of those tables already existed and were empty.</b> <c>UserOffice</c> and
/// <c>StaffCostBaseline</c> were created with the schema and never filled, while the legacy source
/// held 1,949 and 59 rows for them. The earlier migration flattened the office onto the user
/// instead and dropped the cost baselines entirely.
/// </para>
/// <para>
/// <b>Encrypted values are copied as ciphertext.</b> The identity number and the MEDULA password
/// use the same deterministic converter and the same key on both sides, so moving the stored text
/// verbatim is exact — and it means no plaintext is produced, held or logged anywhere in this step.
/// </para>
/// <para>
/// <b>Nothing is dropped here.</b> This copies. The old columns stay where they are until the
/// counts have been verified and the application has been moved over; a step that both moves and
/// deletes leaves nothing to go back to.
/// </para>
/// <para>
/// Idempotent throughout: every insert skips users that already have a row, so a second run writes
/// nothing and a run that dies halfway can simply be repeated.
/// </para>
/// </summary>
public sealed class UserSplitStep : IMigrationStep
{
    public int Order => 27;

    public string Name => "user-split";

    public string Description => "Moves profile, employment, MEDULA, office and cost data off the User table";

    /// <summary>
    /// Legacy staff type to <c>UserType.Code</c>. Read from the data rather than guessed: these are
    /// the nine distinct values <c>Kullanici_T.PersonelTuru</c> actually holds across 3,706 rows.
    /// <c>NCE</c> and the empty value have no counterpart and are reported rather than assigned.
    /// </summary>
    private static readonly (string Legacy, string Code)[] StaffTypes =
    [
        ("Uzman", "UZMAN"),
        ("Doktor", "HEKIM"),
        ("Admin", "KURUM-YONETICISI"),
        ("Müşteri", "MUSTERI"),
        ("Diğer Sağlık", "DSP"),
        ("ofis-admin", "OFIS-YONETICISI"),
        ("Ofis personeli", "BURO"),
        ("ser-admin", "SISTEM-YONETICISI"),
    ];

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.DryRun)
        {
            return await ReportAsync(context, cancellationToken);
        }

        var notes = new List<string>();
        var written = 0;

        // The copies that used to run here -- profile, employment, MEDULA, phone, roles, default
        // office, recovered types -- were one-time bridges out of the User table. Those columns
        // are gone, so the bridges cannot run and are not needed: TenancyStep writes the account
        // and the rows describing the person together now. What remains reads the legacy source.
        written += await StaffTypesAsync(context, notes, cancellationToken);
        written += await OfficesAsync(context, notes, cancellationToken);
        written += await BaselinesAsync(context, notes, cancellationToken);
        written += await CompanyScopeAsync(context, notes, cancellationToken);
        written += await RoleProfilesAsync(context, notes, cancellationToken);

        return new StepResult(written, written, 0, string.Join("; ", notes));
    }

    // ------------------------------------------------------------------ copies out of User


    // ------------------------------------------------------------------ from the legacy source

    /// <summary>
    /// The link that was never made: 3,706 legacy rows carry a staff type and nothing joined a user
    /// to one. <c>StaffRole</c> on the user was the same fact stored a second way.
    /// </summary>
    private async Task<int> StaffTypesAsync(
        MigrationContext context,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var users = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        if (users.Count == 0)
        {
            notes.Add("staff types: no user id map");
            return 0;
        }

        var types = new Dictionary<string, int>(StringComparer.Ordinal);

        await using (var modern = await context.OpenModernAsync(cancellationToken))
        await using (var command = new SqlCommand("SELECT Code, Id FROM ensa.UserType", modern))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                types[reader.GetString(0)] = reader.GetInt32(1);
            }
        }

        var byLegacy = StaffTypes
            .Where(pair => types.ContainsKey(pair.Code))
            .ToDictionary(pair => pair.Legacy, pair => types[pair.Code], StringComparer.OrdinalIgnoreCase);

        var pending = new List<(int UserId, int TypeId)>();
        var unmatched = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        await using (var legacy = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT KullaniciId, PersonelTuru FROM Kullanici_T WHERE PersonelTuru IS NOT NULL", legacy))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (!users.TryGetValue(reader.GetInt32(0), out var userId))
                {
                    continue;
                }

                var legacyType = reader.GetString(1).Trim();

                if (byLegacy.TryGetValue(legacyType, out var typeId))
                {
                    pending.Add((userId, typeId));
                }
                else
                {
                    unmatched[legacyType] = unmatched.GetValueOrDefault(legacyType) + 1;
                }
            }
        }

        var written = await ApplyPairsAsync(
            context,
            "UPDATE target SET UserTypeId = source.TypeId FROM ensa.UserEmployment AS target "
            + "JOIN (VALUES {0}) AS source (UserId, TypeId) ON target.UserId = source.UserId "
            + "WHERE target.UserTypeId IS NULL",
            pending,
            cancellationToken);

        var note = $"user types: {written} assigned";
        if (unmatched.Count > 0)
        {
            note += ", unmatched " + string.Join(", ", unmatched.Select(x => $"{x.Key} x{x.Value}"));
        }

        notes.Add(note);
        return written;
    }

    /// <summary>
    /// <c>KullaniciOfis_T</c>, 1,949 rows that were never carried across. The earlier migration put
    /// a single <c>OfficeId</c> on the user instead, which cannot express a specialist who works in
    /// two offices — and the legacy data says many of them do.
    /// </summary>
    private static async Task<int> OfficesAsync(
        MigrationContext context,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var users = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var offices = await context.IdMap.LoadAsync("Ofisler_T", cancellationToken);

        if (users.Count == 0 || offices.Count == 0)
        {
            notes.Add("offices: no id map");
            return 0;
        }

        var pending = new List<(int UserId, int OfficeId, int Minutes)>();
        var orphaned = 0;

        await using (var legacy = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT KullaniciId, OfisId, ISNULL(Sure, 0) FROM KullaniciOfis_T WHERE Aktif = 1", legacy))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (users.TryGetValue(reader.GetInt32(0), out var userId)
                    && offices.TryGetValue(reader.GetInt32(1), out var officeId))
                {
                    pending.Add((userId, officeId, reader.GetInt32(2)));
                }
                else
                {
                    orphaned++;
                }
            }
        }

        var written = 0;

        await using (var connection = await context.OpenModernAsync(cancellationToken))
        {
            foreach (var chunk in pending.Chunk(400))
            {
                var values = string.Join(",",
                    chunk.Select((_, i) => $"(@u{i},@o{i},@m{i})"));

                await using var command = new SqlCommand(
                    $"""
                     INSERT INTO ensa.UserOffice (UserId, OfficeId, MonthlyWorkDurationMinutes, TenantId, CreationTime, CreatorId)
                     SELECT source.UserId, source.OfficeId, source.Minutes, u.TenantId, SYSDATETIME(), u.CreatorId
                     FROM (VALUES {values}) AS source (UserId, OfficeId, Minutes)
                     JOIN ensa.[User] AS u ON u.Id = source.UserId
                     WHERE NOT EXISTS (
                         SELECT 1 FROM ensa.UserOffice AS existing
                         WHERE existing.UserId = source.UserId AND existing.OfficeId = source.OfficeId);
                     """, connection) { CommandTimeout = 600 };

                for (var i = 0; i < chunk.Length; i++)
                {
                    command.Parameters.AddWithValue($"@u{i}", chunk[i].UserId);
                    command.Parameters.AddWithValue($"@o{i}", chunk[i].OfficeId);
                    command.Parameters.AddWithValue($"@m{i}", chunk[i].Minutes);
                }

                written += await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        notes.Add($"offices: {written} of {pending.Count + orphaned} rows"
                  + (orphaned > 0 ? $", {orphaned} unresolvable" : string.Empty));

        return written;
    }

    /// <summary>
    /// <c>BazalKullanici_T</c>, 59 rows. Small, and dropped entirely by the earlier migration:
    /// these are the frozen cost figures a past period was billed against, so losing them means
    /// losing the ability to explain an old invoice.
    /// </summary>
    private static async Task<int> BaselinesAsync(
        MigrationContext context,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var users = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var offices = await context.IdMap.LoadAsync("Ofisler_T", cancellationToken);

        var pending = new List<(int? UserId, int OfficeId, string FullName, decimal Salary, decimal Ssi,
            int Minutes, int UsedMinutes, int? WorkedDays, bool Meal, bool Active, DateTime? HireDate)>();
        var orphaned = 0;

        await using (var legacy = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            """
            SELECT IliskiliKullaniciId, OfisId, ISNULL(AdSoyad, ''), ISNULL(Maas, 0),
                   ISNULL(SGKTutari, 0), ISNULL(IsgKatipDk, 0), ISNULL(IsgKatipKulDk, 0),
                   CalisilanGun, ISNULL(YemekliMi, 0), ISNULL(Aktif, 1), IseGirisTarihi
            FROM BazalKullanici_T
            """, legacy))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var legacyUser = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0);
                var legacyOffice = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);

                if (!offices.TryGetValue(legacyOffice, out var officeId))
                {
                    orphaned++;
                    continue;
                }

                int? userId = legacyUser is { } id && users.TryGetValue(id, out var mapped) ? mapped : null;

                pending.Add((userId, officeId, reader.GetString(2),
                    reader.GetDecimal(3), reader.GetDecimal(4), reader.GetInt32(5),
                    reader.GetInt32(6),
                    reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    reader.GetBoolean(8), reader.GetBoolean(9),
                    reader.IsDBNull(10) ? null : reader.GetDateTime(10)));
            }
        }

        var written = 0;

        await using (var connection = await context.OpenModernAsync(cancellationToken))
        {
            foreach (var row in pending)
            {
                await using var command = new SqlCommand(
                    """
                    INSERT INTO ensa.StaffCostBaseline
                        (UserId, OfficeId, FullName, StaffRole, HireDate, Salary, SsiAmount,
                         WorkedDayCount, OhsKatipMinutes, OhsKatipUsedMinutes, IncludesMeal,
                         IsActive, TenantId, CreationTime, IsDeleted)
                    SELECT @userId, @officeId, @fullName, 0, @hireDate, @salary, @ssi, @workedDays,
                           @minutes, @usedMinutes, @meal, @active,
                           (SELECT TOP 1 TenantId FROM ensa.Office WHERE Id = @officeId),
                           SYSDATETIME(), 0
                    WHERE NOT EXISTS (
                        SELECT 1 FROM ensa.StaffCostBaseline
                        WHERE OfficeId = @officeId AND FullName = @fullName
                          AND (UserId = @userId OR (UserId IS NULL AND @userId IS NULL)));
                    """, connection) { CommandTimeout = 600 };

                command.Parameters.AddWithValue("@userId", (object?)row.UserId ?? DBNull.Value);
                command.Parameters.AddWithValue("@officeId", row.OfficeId);
                command.Parameters.AddWithValue("@fullName", row.FullName);
                command.Parameters.AddWithValue("@salary", row.Salary);
                command.Parameters.AddWithValue("@ssi", row.Ssi);
                command.Parameters.AddWithValue("@minutes", row.Minutes);
                command.Parameters.AddWithValue("@usedMinutes", row.UsedMinutes);
                command.Parameters.AddWithValue("@workedDays", (object?)row.WorkedDays ?? DBNull.Value);
                command.Parameters.AddWithValue("@meal", row.Meal);
                command.Parameters.AddWithValue("@active", row.Active);
                command.Parameters.AddWithValue("@hireDate", (object?)row.HireDate ?? DBNull.Value);

                written += await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        notes.Add($"cost baselines: {written} of {pending.Count + orphaned} rows"
                  + (orphaned > 0 ? $", {orphaned} unresolvable" : string.Empty));

        return written;
    }

    /// <summary>
    /// The company a <b>customer</b> user belongs to, resolved from the legacy account.
    ///
    /// <para>
    /// It is the key the company scope filter reads, and that filter fails closed: a user who
    /// carries a company sees that company and nothing else. So the question this answers is not
    /// "does the legacy row have a <c>FirmaId</c>" — it is "is this person a customer contact".
    /// </para>
    ///
    /// <para>
    /// <b>The two are not the same, and treating them as the same was a defect.</b>
    /// <c>Kullanici_T.FirmaId</c> was populated for our own staff as well: 731 of the 766 legacy
    /// <c>Admin</c> accounts had one, and 728 of those pointed at the organization's <i>own</i>
    /// company record rather than at a customer workplace. The legacy application never read
    /// <c>FirmaId</c> to decide what an administrator could see — its company list filtered on
    /// <c>KurumId</c> and <c>OfisId</c> alone, and <c>BaseController.FirmaId</c> only fell back to
    /// the column for <c>PersonelTuru == "Personel"</c>. Copying it across for everyone therefore
    /// pinned 983 members of staff — 713 of them organization administrators — to a single
    /// workplace, and each of them saw exactly one company where they should have seen the whole
    /// organization's.
    /// </para>
    ///
    /// <para>
    /// Idempotent: only profiles whose <c>CompanyId</c> is still null are written, so a second run
    /// changes nothing. It never clears a value either — widening the scope of an account that
    /// already carries one is not this step's decision to make, and repairing data that a previous
    /// run of the defective version already wrote is
    /// <see cref="CompanyScopeRepairStep"/>'s job.
    /// </para>
    /// </summary>
    private static async Task<int> CompanyScopeAsync(
        MigrationContext context,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var users = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var companies = await context.IdMap.LoadAsync("Firma_T", cancellationToken);

        if (users.Count == 0 || companies.Count == 0)
        {
            notes.Add("company scope: no id map");
            return 0;
        }

        var pending = new List<(int UserId, int CompanyId)>();
        var unresolved = 0;

        await using (var legacy = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT KullaniciId, FirmaId, PersonelTuru FROM Kullanici_T "
            + "WHERE FirmaId IS NOT NULL AND PersonelTuru IS NOT NULL", legacy))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (!LegacyStaffType.IsCustomer(reader.GetString(2)))
                {
                    continue;
                }

                if (!users.TryGetValue(reader.GetInt32(0), out var userId))
                {
                    continue;
                }

                if (!companies.TryGetValue(reader.GetInt32(1), out var companyId))
                {
                    // A customer whose workplace did not survive is left unscoped rather than
                    // pointed at nothing: the filter fails closed, so a wrong id would blind them.
                    unresolved++;
                    continue;
                }

                pending.Add((userId, companyId));
            }
        }

        var written = await ApplyPairsAsync(
            context,
            "UPDATE target SET CompanyId = source.CompanyId FROM ensa.UserProfile AS target "
            + "JOIN (VALUES {0}) AS source (UserId, CompanyId) ON target.UserId = source.UserId "
            + "WHERE target.CompanyId IS NULL",
            pending,
            cancellationToken);

        context.Logger.LogInformation(
            "    company scope: {Written} customer user(s) of {Candidates}", written, pending.Count);

        var note = $"company scope: {written} of {pending.Count} customer users";
        if (unresolved > 0)
        {
            note += $", {unresolved} with an unresolvable workplace";
        }

        notes.Add(note);
        return written;
    }

    /// <summary>
    /// A role's description and its two behaviour flags. Identity's role table carries a name and
    /// a concurrency stamp; everything else is ours and belongs in our own table.
    /// </summary>
    private static async Task<int> RoleProfilesAsync(
        MigrationContext context,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        await using var connection = await context.OpenModernAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            INSERT INTO ensa.RoleProfile (RoleId, Description, IsStatic, IsDefault, CreationTime)
            SELECT r.Id, r.Description, r.IsStatic, r.IsDefault, SYSDATETIME()
            FROM ensa.Role AS r
            WHERE NOT EXISTS (SELECT 1 FROM ensa.RoleProfile AS p WHERE p.RoleId = r.Id);
            """, connection) { CommandTimeout = 600 };

        var written = await command.ExecuteNonQueryAsync(cancellationToken);

        context.Logger.LogInformation("    role profiles: {Written}", written);
        notes.Add($"role profiles: {written}");

        return written;
    }

    // ------------------------------------------------------------------ plumbing

    private static async Task<int> ApplyPairsAsync(
        MigrationContext context,
        string template,
        List<(int First, int Second)> pending,
        CancellationToken cancellationToken)
    {
        if (pending.Count == 0)
        {
            return 0;
        }

        var written = 0;
        await using var connection = await context.OpenModernAsync(cancellationToken);

        foreach (var chunk in pending.Chunk(600))
        {
            var values = string.Join(",", chunk.Select((_, i) => $"(@a{i},@b{i})"));

            await using var command = new SqlCommand(
                string.Format(System.Globalization.CultureInfo.InvariantCulture, template, values),
                connection) { CommandTimeout = 1800 };

            for (var i = 0; i < chunk.Length; i++)
            {
                command.Parameters.AddWithValue($"@a{i}", chunk[i].First);
                command.Parameters.AddWithValue($"@b{i}", chunk[i].Second);
            }

            written += await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return written;
    }

    /// <summary>Counts what a real run would move, without moving it.</summary>
    private static async Task<StepResult> ReportAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var connection = await context.OpenModernAsync(cancellationToken);
        await using var command = new SqlCommand(
            """
            SELECT
                (SELECT COUNT(*) FROM ensa.[User] u
                 WHERE NOT EXISTS (SELECT 1 FROM ensa.UserProfile p WHERE p.UserId = u.Id)),
                (SELECT COUNT(*) FROM ensa.[User] u
                 WHERE NOT EXISTS (SELECT 1 FROM ensa.UserEmployment e WHERE e.UserId = u.Id)),
                (SELECT COUNT(*) FROM ensa.[User] u
                 WHERE (u.MedulaUserName IS NOT NULL OR u.MedulaPassword IS NOT NULL OR u.BranchCode IS NOT NULL)
                   AND NOT EXISTS (SELECT 1 FROM ensa.UserMedulaCredential m WHERE m.UserId = u.Id))
            """, connection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        var profiles = reader.GetInt32(0);
        var employments = reader.GetInt32(1);
        var medula = reader.GetInt32(2);

        return new StepResult(profiles + employments + medula, 0, 0,
            $"dry run: {profiles} profiles, {employments} employments, {medula} MEDULA credentials");
    }
}
