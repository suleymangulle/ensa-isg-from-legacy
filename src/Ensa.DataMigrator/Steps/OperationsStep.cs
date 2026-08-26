using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Companies;
using Ensa.Domain.Risks;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The equipment on each workplace's inventory, and the specialists assigned to serve it.
/// <para>
/// These two are what make the rest of the OHS record meaningful: a periodic inspection is about a
/// machine, and every visit, plan and report is signed by somebody assigned to the workplace.
/// </para>
/// </summary>
public sealed class OperationsStep : IMigrationStep
{
    public int Order => 40;

    public string Name => "operations";

    public string Description => "Equipment inventory and the specialists assigned to each workplace";

    private const int BatchSize = 500;

    /// <summary>
    /// Legacy <c>CihazTuru</c> to <see cref="EquipmentType"/>.
    /// <para>
    /// The legacy vocabulary has two values covering the whole inventory; the rebuilt enum has six.
    /// Nothing is invented to fill the other four — a lifting appliance recorded as
    /// <c>tesisat-techizat</c> stays installation equipment until somebody reclassifies it, because
    /// guessing would put a machine in a category its inspection rules do not match.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, EquipmentType> EquipmentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["makine-tezgah"] = EquipmentType.MachineBench,
            ["tesisat-techizat"] = EquipmentType.InstallationEquipment,
        };

    /// <summary>Legacy <c>PersonelTuru</c> on an assignment to <see cref="StaffRole"/>.</summary>
    private static readonly Dictionary<string, StaffRole> StaffRoles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Uzman"] = StaffRole.OccupationalSafetySpecialist,
            ["Doktor"] = StaffRole.WorkplacePhysician,
            ["Hekim"] = StaffRole.WorkplacePhysician,
            ["Diger Saglik"] = StaffRole.OtherHealthPersonnel,
            ["Diğer Sağlık"] = StaffRole.OtherHealthPersonnel,
            ["Admin"] = StaffRole.OrganizationAdministrator,
        };

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var read = 0;
        var written = 0;
        var skipped = 0;
        var notes = new List<string>();

        var equipment = await MigrateEquipmentAsync(context, cancellationToken);
        read += equipment.Read;
        written += equipment.Written;
        skipped += equipment.Skipped;
        notes.Add(equipment.Note!);

        var assignments = await MigrateAssignedSpecialistsAsync(context, cancellationToken);
        read += assignments.Read;
        written += assignments.Written;
        skipped += assignments.Skipped;
        notes.Add(assignments.Note!);

        return new StepResult(read, written, skipped, string.Join("; ", notes));
    }

    // ------------------------------------------------------------------ equipment

    private static async Task<StepResult> MigrateEquipmentAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var already = await context.IdMap.LoadAsync("Cihaz_T", cancellationToken);

        var read = 0;
        var written = 0;
        var orphaned = 0;
        var unknownType = 0;
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, Equipment Entity)>();

        const string sql = """
            SELECT CihazId, FirmaId, CihazAdi, CihazTuru, PerMuayeneRaporu, PerMuayeneYapan,
                   MuayeneTarihi, SonrakiMuayeneTarihi, PeriyotId, Deletable, KurumId, IsDeleted
            FROM Cihaz_T ORDER BY CihazId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "CihazId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId))
                {
                    orphaned++;
                    continue;
                }

                var typeText = Text(reader, "CihazTuru");
                if (typeText is null || !EquipmentTypes.TryGetValue(typeText, out var equipmentType))
                {
                    equipmentType = EquipmentType.Unspecified;
                    unknownType++;
                }

                batch.Add((legacyId, new Equipment
                {
                    CompanyId = companyId,
                    EquipmentName = Fit(context, "Equipment", "EquipmentName", Text(reader, "CihazAdi"))
                                    ?? $"Equipment {legacyId}",
                    EquipmentType = equipmentType,
                    ExaminationReport = Fit(context, "Equipment", "ExaminationReport", Text(reader, "PerMuayeneRaporu")),
                    ExaminationPerformedBy =
                        Fit(context, "Equipment", "ExaminationPerformedBy", Text(reader, "PerMuayeneYapan")),
                    ExaminationDate = Date(reader, "MuayeneTarihi"),
                    NextExaminationDate = Date(reader, "SonrakiMuayeneTarihi"),
                    // PeriyotId points at the legacy Periyot_T catalogue, which is not migrated yet.
                    // Carrying the number across would make it point at an unrelated row.
                    PeriodId = null,
                    Deletable = Bit(reader, "Deletable"),
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId)
                        ? tenantId
                        : null,
                    IsDeleted = Bit(reader, "IsDeleted"),
                }));

                if (batch.Count >= BatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, "Cihaz_T", batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, "Cihaz_T", batch, pairs, cancellationToken);
        }

        var note = $"equipment: {written} written";
        if (unknownType > 0)
        {
            note += $", {unknownType} unrecognised type -> Unspecified";
        }

        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (company missing)";
        }

        return new StepResult(read, written, read - written, note);
    }

    // ------------------------------------------------------------------ assigned specialists

    private static async Task<StepResult> MigrateAssignedSpecialistsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var userMap = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var already = await context.IdMap.LoadAsync("FirmaIlgilenen_T", cancellationToken);

        // (company, user, role) is unique in the rebuilt schema: one person serves a workplace in
        // one capacity. The legacy table records the assignment again each time it is renewed.
        var seen = new Dictionary<(int Company, int User, StaffRole Role), int>();

        foreach (var existing in await db.Set<AssignedSpecialist>()
                     .Select(a => new { a.Id, a.CompanyId, a.UserId, a.StaffRole })
                     .ToListAsync(cancellationToken))
        {
            seen[(existing.CompanyId, existing.UserId, existing.StaffRole)] = existing.Id;
        }

        var read = 0;
        var written = 0;
        var orphaned = 0;
        var duplicates = 0;
        var unknownRole = 0;
        var pairs = new List<(int, int)>();
        var mergedPairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, AssignedSpecialist Entity)>();

        const string sql = """
            SELECT FirmaIlgilenenId, KullaniciId, FirmaId, PersonelTuru, AylikCalismaSuresi,
                   SID, Aktif, IsgProfOnay, IsgProfOnayGuid, KurumId
            FROM FirmaIlgilenen_T ORDER BY FirmaIlgilenenId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "FirmaIlgilenenId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !userMap.TryGetValue(Required(reader, "KullaniciId"), out var userId))
                {
                    orphaned++;
                    continue;
                }

                var roleText = Text(reader, "PersonelTuru");
                if (roleText is null || !StaffRoles.TryGetValue(roleText, out var staffRole))
                {
                    staffRole = StaffRole.Unspecified;
                    unknownRole++;
                }

                var key = (companyId, userId, staffRole);
                if (seen.TryGetValue(key, out var existingId))
                {
                    // The same person, workplace and capacity recorded again. The repeat legacy id
                    // points at the one assignment rather than disappearing, so anything referring
                    // to it still resolves.
                    if (existingId > 0)
                    {
                        mergedPairs.Add((legacyId, existingId));
                    }

                    duplicates++;
                    continue;
                }

                // Reserved before the insert, so two rows inside one batch cannot both pass.
                seen[key] = 0;

                batch.Add((legacyId, new AssignedSpecialist
                {
                    CompanyId = companyId,
                    UserId = userId,
                    StaffRole = staffRole,
                    MonthlyWorkDurationMinutes = Int(reader, "AylikCalismaSuresi"),
                    Sid = Fit(context, "AssignedSpecialist", "Sid", Text(reader, "SID")),
                    IsActive = Bit(reader, "Aktif"),
                    OhsProfApproval = Bit(reader, "IsgProfOnay"),
                    OhsProfApprovalGuid = Fit(context, "AssignedSpecialist", "OhsProfApprovalGuid",
                        Text(reader, "IsgProfOnayGuid")),
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId)
                        ? tenantId
                        : null,
                }));

                if (batch.Count >= BatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, "FirmaIlgilenen_T", batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, "FirmaIlgilenen_T", batch, pairs, cancellationToken);
        }

        if (mergedPairs.Count > 0 && !context.DryRun)
        {
            await context.IdMap.SaveAsync("FirmaIlgilenen_T", mergedPairs, 'M', cancellationToken);
        }

        var note = $"assigned specialists: {written} written";
        if (duplicates > 0)
        {
            note += $", {duplicates} repeat assignment(s) of the same person and capacity collapsed";
        }

        if (unknownRole > 0)
        {
            note += $", {unknownRole} unrecognised staff role -> Unspecified";
        }

        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (company or user missing)";
        }

        return new StepResult(read, written, read - written, note);
    }

    // ------------------------------------------------------------------ shared

    /// <summary>
    /// Every legacy company id, whether it became a client workplace or an organization's own
    /// company record. Both are <c>Company</c> rows, and a legacy foreign key does not distinguish.
    /// </summary>
    private static async Task<Dictionary<int, int>> LoadCompanyMapAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, int>(await context.IdMap.LoadAsync("Firma_T", cancellationToken));

        foreach (var (legacyId, modernId) in
                 await context.IdMap.LoadAsync("Firma_T:KurumSirket", cancellationToken))
        {
            map[legacyId] = modernId;
        }

        return map;
    }

    private static async Task<int> FlushAsync<TEntity>(
        DbContext db,
        MigrationContext context,
        string legacyTable,
        List<(int LegacyId, TEntity Entity)> batch,
        List<(int, int)> pairs,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        db.Set<TEntity>().AddRange(batch.Select(item => item.Entity));
        await db.SaveChangesAsync(cancellationToken);

        var chunkPairs = batch
            .Select(item => (item.LegacyId, (int)db.Entry(item.Entity).Property("Id").CurrentValue!))
            .ToList();

        await context.IdMap.SaveAsync(legacyTable, chunkPairs, 'I', cancellationToken);
        pairs.AddRange(chunkPairs);

        var count = batch.Count;
        batch.Clear();
        db.ChangeTracker.Clear();

        return count;
    }

    private static int DryRunFlush<TEntity>(
        MigrationContext context,
        List<(int LegacyId, TEntity Entity)> batch,
        List<(int, int)> pairs)
    {
        pairs.AddRange(batch.Select(item => (item.LegacyId, context.NextDryRunId())));

        var count = batch.Count;
        batch.Clear();
        return count;
    }

    // ------------------------------------------------------------------ readers

    private static string? Fit(MigrationContext context, string table, string column, string? value)
        => context.Fitter.Fit(table, column, value);

    private static string? Text(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = reader.GetValue(index)?.ToString()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static int? Int(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : Convert.ToInt32(reader.GetValue(index));
    }

    private static bool Bit(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return !reader.IsDBNull(index) && Convert.ToBoolean(reader.GetValue(index));
    }

    private static DateTime? Date(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }

    private static int Required(SqlDataReader reader, string column)
        => reader.GetInt32(reader.GetOrdinal(column));
}
