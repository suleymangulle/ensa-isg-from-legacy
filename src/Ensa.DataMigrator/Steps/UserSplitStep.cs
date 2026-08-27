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

        written += await Run(context, "UserProfile", ProfileSql, notes, cancellationToken);
        written += await Run(context, "UserEmployment", EmploymentSql, notes, cancellationToken);
        written += await Run(context, "UserMedulaCredential", MedulaSql, notes, cancellationToken);
        written += await Run(context, "PhoneNumber", PhoneSql, notes, cancellationToken);

        written += await RolesAsync(context, notes, cancellationToken);
        written += await StaffTypesAsync(context, notes, cancellationToken);
        written += await OfficesAsync(context, notes, cancellationToken);
        written += await BaselinesAsync(context, notes, cancellationToken);

        return new StepResult(written, written, 0, string.Join("; ", notes));
    }

    // ------------------------------------------------------------------ copies out of User

    /// <summary>
    /// The person. <c>NationalId</c> moves as stored ciphertext — same converter, same key, so the
    /// value is identical without ever being decrypted.
    /// </summary>
    private const string ProfileSql =
        """
        INSERT INTO ensa.UserProfile
            (UserId, TenantId, Name, LastName, NationalId, Address, CityId, DistrictId,
             PhotoDocumentId, Color, IsActive, MustChangePassword, ContractApproved,
             CreationTime, CreatorId, IsDeleted)
        SELECT u.Id, u.TenantId, u.Name, u.LastName, u.NationalId, u.Address, u.CityId, u.DistrictId,
               u.PhotoDocumentId, u.Color, u.IsActive, u.MustChangePassword, u.ContractApproved,
               SYSDATETIME(), u.CreatorId, u.IsDeleted
        FROM ensa.[User] AS u
        WHERE NOT EXISTS (SELECT 1 FROM ensa.UserProfile AS p WHERE p.UserId = u.Id);
        """;

    /// <summary>The contract. <c>UserTypeId</c> is filled separately, from the legacy staff type.</summary>
    private const string EmploymentSql =
        """
        INSERT INTO ensa.UserEmployment
            (UserId, TenantId, HireDate, TerminationDate, GrossSalary, PartTime,
             CreationTime, CreatorId, IsDeleted)
        SELECT u.Id, u.TenantId, u.HireDate, u.TerminationDate, u.GrossSalary, u.PartTime,
               SYSDATETIME(), u.CreatorId, u.IsDeleted
        FROM ensa.[User] AS u
        WHERE NOT EXISTS (SELECT 1 FROM ensa.UserEmployment AS e WHERE e.UserId = u.Id);
        """;

    /// <summary>
    /// Another system's credentials, and only for the users that have any: 297 of 3,886 carry a
    /// MEDULA login, and giving the other 3,589 an empty row would be storing nothing, 3,589 times.
    /// </summary>
    private const string MedulaSql =
        """
        INSERT INTO ensa.UserMedulaCredential
            (UserId, TenantId, MedulaUserName, MedulaPassword, BranchCode,
             CreationTime, CreatorId, IsDeleted)
        SELECT u.Id, u.TenantId, u.MedulaUserName, u.MedulaPassword, u.BranchCode,
               SYSDATETIME(), u.CreatorId, u.IsDeleted
        FROM ensa.[User] AS u
        WHERE (u.MedulaUserName IS NOT NULL OR u.MedulaPassword IS NOT NULL OR u.BranchCode IS NOT NULL)
          AND NOT EXISTS (SELECT 1 FROM ensa.UserMedulaCredential AS m WHERE m.UserId = u.Id);
        """;

    /// <summary>
    /// <c>Gsm</c> and Identity's own <c>PhoneNumber</c> were two columns holding the same fact.
    /// The framework's one wins; the other is folded into it where it is empty, so nothing is lost.
    /// </summary>
    private const string PhoneSql =
        """
        UPDATE ensa.[User]
        SET PhoneNumber = Gsm
        WHERE Gsm IS NOT NULL AND LEN(Gsm) > 0
          AND (PhoneNumber IS NULL OR LEN(PhoneNumber) = 0);
        """;

    /// <summary>
    /// Turns the three administrator booleans into the role assignments they already behave like.
    /// <para>
    /// <b>Why roles and not another column.</b> The token already converts these flags into role
    /// claims — the comment on that code says it exists "so that TenantResolutionMiddleware and the
    /// policies all look at one source: the role claim". They are roles in everything but storage.
    /// Identity owns roles; putting them on a profile table instead would duplicate what
    /// <c>UserRole</c> is for, which the identity contract forbids.
    /// </para>
    /// <para>
    /// <b>Why these three and not the legacy permission flags.</b> The contract also says not to
    /// turn every legacy authorization flag into a role. These are not permissions: they appear
    /// nowhere in <c>Yetki_T</c> and the four gates never consult them. What they are is the answer
    /// to "what is this person", which is what a role is.
    /// </para>
    /// <para>
    /// The roles already exist and are host level: SystemAdministrator, OrganizationAdministrator,
    /// OfficeAdministrator. Behaviour does not change — the same users end up with the same claims,
    /// they just come from the table Identity keeps them in.
    /// </para>
    /// </summary>
    private static async Task<int> RolesAsync(
        MigrationContext context,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        (string Column, string Role)[] flags =
        [
            ("SystemAdministrator", "SystemAdministrator"),
            ("OrganizationAdmin", "OrganizationAdministrator"),
            ("OfficeAdmin", "OfficeAdministrator"),
        ];

        var written = 0;
        await using var connection = await context.OpenModernAsync(cancellationToken);

        foreach (var (column, role) in flags)
        {
            await using var command = new SqlCommand(
                $"""
                 INSERT INTO ensa.UserRole (UserId, RoleId)
                 SELECT u.Id, r.Id
                 FROM ensa.[User] AS u
                 CROSS JOIN (SELECT TOP 1 Id FROM ensa.Role WHERE Name = @role AND TenantId IS NULL) AS r
                 WHERE u.[{column}] = 1
                   AND NOT EXISTS (
                       SELECT 1 FROM ensa.UserRole AS existing
                       WHERE existing.UserId = u.Id AND existing.RoleId = r.Id);
                 """, connection) { CommandTimeout = 600 };

            command.Parameters.AddWithValue("@role", role);

            var count = await command.ExecuteNonQueryAsync(cancellationToken);
            written += count;

            context.Logger.LogInformation("    role {Role}: {Count} assignment(s)", role, count);
        }

        notes.Add($"roles: {written} assignments");
        return written;
    }

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

    // ------------------------------------------------------------------ plumbing

    private static async Task<int> Run(
        MigrationContext context,
        string label,
        string sql,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        await using var connection = await context.OpenModernAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 };

        var written = await command.ExecuteNonQueryAsync(cancellationToken);
        context.Logger.LogInformation("    {Label}: {Written} row(s)", label, written);
        notes.Add($"{label}: {written}");

        return written;
    }

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
