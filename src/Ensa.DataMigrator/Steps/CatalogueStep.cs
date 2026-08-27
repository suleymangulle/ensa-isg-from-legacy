using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Lookups;
using Ensa.Domain.Plans;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Trainings;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The activity and training catalogues the plans are built from.
/// <para>
/// Small tables — a few hundred rows — but nothing else can move without them: every work plan line
/// names an activity and every training plan line names a training, and both columns are required.
/// </para>
/// <para>
/// <b>Two legacy columns look like enums and are not.</b> <c>Aktivite_T.Tur</c> and
/// <c>Egitim_T.Tur</c> hold <c>"Uzman"</c>, <c>"Doktor"</c> and a scattering of numbers: they record
/// which staff role performs the item, not what kind of item it is. The rebuilt
/// <see cref="ActivityType"/> means something else entirely, so it is left at its default rather
/// than filled with a value that happens to be in the same-named column.
/// </para>
/// <para>
/// <b>One mapping does land exactly.</b> A training's subject group is three legacy booleans —
/// <c>GenelKonular</c>, <c>SaglikKonulari</c>, <c>TeknikKonular</c> — and the rebuilt
/// <see cref="TrainingSubjectGroup"/> is the same three values. It matters more than it looks:
/// <c>CompanyComplianceCalculator</c> splits safety training from health training on exactly this
/// field, so getting it wrong would misreport every company's outstanding obligations.
/// </para>
/// </summary>
public sealed class CatalogueStep : IMigrationStep
{
    public int Order => 15;

    public string Name => "catalogues";

    public string Description => "Activity and training catalogues, and the periods they use";

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var read = 0;
        var written = 0;
        var notes = new List<string>();

        foreach (var result in new[]
                 {
                     await MigratePeriodsAsync(context, cancellationToken),
                     await MigrateActivityGroupsAsync(context, cancellationToken),
                     await MigrateActivitiesAsync(context, cancellationToken),
                     await MigrateTrainingGroupsAsync(context, cancellationToken),
                     await MigrateTrainingsAsync(context, cancellationToken),
                 })
        {
            read += result.Read;
            written += result.Written;
            notes.Add(result.Note!);
        }

        return new StepResult(read, written, read - written, string.Join("; ", notes));
    }

    // ------------------------------------------------------------------ periods

    private static async Task<StepResult> MigratePeriodsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();
        var already = await context.IdMap.LoadAsync("Periyot_T", cancellationToken);

        var rows = new List<(int LegacyId, Period Entity)>();
        var read = 0;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT PeriyotId, PeriyotAdi, PeriyotDegeri, PeriyotExpression FROM Periyot_T ORDER BY PeriyotId",
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;
                var legacyId = Required(reader, "PeriyotId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                rows.Add((legacyId, new Period
                {
                    PeriodName = Fit(context, "Period", "PeriodName", Text(reader, "PeriyotAdi")) ?? $"Period {legacyId}",
                    PeriodValue = Int(reader, "PeriyotDegeri") ?? 0,
                    // The legacy table has no unit column; the value and the expression carry it
                    // together. Month is the unit every legacy period actually uses.
                    PeriodUnit = PeriodUnit.Month,
                    PeriodExpression = Fit(context, "Period", "PeriodExpression", Text(reader, "PeriyotExpression")),
                }));
            }
        }

        var written = await SaveAsync(db, context, "Periyot_T", rows, cancellationToken);
        return new StepResult(read, written, 0, $"periods: {written} written");
    }

    // ------------------------------------------------------------------ activity groups

    private static async Task<StepResult> MigrateActivityGroupsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();
        var already = await context.IdMap.LoadAsync("AktiviteGrup_T", cancellationToken);

        var rows = new List<(int LegacyId, ActivityGroup Entity)>();
        var read = 0;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT AktiviteGrupId, GrupAdi, Aktif FROM AktiviteGrup_T ORDER BY AktiviteGrupId", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;
                var legacyId = Required(reader, "AktiviteGrupId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                rows.Add((legacyId, new ActivityGroup
                {
                    GroupName = Fit(context, "ActivityGroup", "GroupName", Text(reader, "GrupAdi"))
                                ?? $"Group {legacyId}",
                    IsActive = Bit(reader, "Aktif"),
                }));
            }
        }

        var written = await SaveAsync(db, context, "AktiviteGrup_T", rows, cancellationToken);
        return new StepResult(read, written, 0, $"activity groups: {written} written");
    }

    // ------------------------------------------------------------------ activities

    private static async Task<StepResult> MigrateActivitiesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var groupMap = await context.IdMap.LoadAsync("AktiviteGrup_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var already = await context.IdMap.LoadAsync("Aktivite_T", cancellationToken);

        var rows = new List<(int LegacyId, Activity Entity)>();
        var parents = new List<(int LegacyId, int LegacyParentId)>();
        var read = 0;

        const string sql = """
            SELECT AktiviteId, UstAktiviteId, AktiviteKodu, AktiviteAdi, GrupId, DefaultAktivite,
                   DefaultAdet, DefaultBaslangicAyKaydirma, DefaultElemanSarti, Aktif,
                   IliskiliTablo, IliskiId, Sira, KurumId
            FROM Aktivite_T ORDER BY AktiviteId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;
                var legacyId = Required(reader, "AktiviteId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                if (Int(reader, "UstAktiviteId") is { } parentId)
                {
                    parents.Add((legacyId, parentId));
                }

                rows.Add((legacyId, new Activity
                {
                    ActivityCode = Fit(context, "Activity", "ActivityCode", Text(reader, "AktiviteKodu")),
                    ActivityName = Fit(context, "Activity", "ActivityName", Text(reader, "AktiviteAdi"))
                                   ?? $"Activity {legacyId}",
                    ActivityGroupId = MapId(groupMap, Int(reader, "GrupId")),
                    // Aktivite_T.Tur holds "Uzman"/"Doktor" - which staff role performs the item,
                    // not what kind of item it is. ActivityType means something else, so it keeps
                    // its default rather than taking a value from a same-named column.
                    ActivityType = ActivityType.Activity,
                    DefaultActivity = Bit(reader, "DefaultAktivite"),
                    DefaultCount = Int(reader, "DefaultAdet") ?? 0,
                    DefaultStartMonthOffset = Int(reader, "DefaultBaslangicAyKaydirma") ?? 0,
                    DefaultElementCondition = Int(reader, "DefaultElemanSarti") ?? 0,
                    IsActive = Bit(reader, "Aktif"),
                    RelatedTable = Fit(context, "Activity", "RelatedTable", Text(reader, "IliskiliTablo")),
                    RelationId = Int(reader, "IliskiId"),
                    OrderNo = Int(reader, "Sira"),
                    TenantId = MapId(organizationMap, Int(reader, "KurumId")),
                }));
            }
        }

        var written = await SaveAsync(db, context, "Aktivite_T", rows, cancellationToken);

        // Parent links need every activity to exist first - a child may be read before its parent.
        var linked = 0;
        if (!context.DryRun && parents.Count > 0)
        {
            var map = await context.IdMap.LoadAsync("Aktivite_T", cancellationToken);

            var links = parents
                .Where(pair => map.ContainsKey(pair.LegacyId) && map.ContainsKey(pair.LegacyParentId))
                .Select(pair => (map[pair.LegacyId], map[pair.LegacyParentId]))
                .ToList();

            linked = await ApplyLinksAsync(context, "ensa.Activity", "ParentActivityId", links, cancellationToken);
        }

        var note = $"activities: {written} written";
        if (linked > 0)
        {
            note += $", {linked} linked to a parent activity";
        }

        return new StepResult(read, written, 0, note);
    }

    // ------------------------------------------------------------------ training groups

    private static async Task<StepResult> MigrateTrainingGroupsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();
        var already = await context.IdMap.LoadAsync("EgitimGrubu_T", cancellationToken);

        var rows = new List<(int LegacyId, TrainingGroup Entity)>();
        var read = 0;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT EgitimGrubuId, EgitimGrubuAdi, EgitimGrubuKodu, Sira FROM EgitimGrubu_T ORDER BY EgitimGrubuId",
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;
                var legacyId = Required(reader, "EgitimGrubuId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                rows.Add((legacyId, new TrainingGroup
                {
                    TrainingGroupName = Fit(context, "TrainingGroup", "TrainingGroupName", Text(reader, "EgitimGrubuAdi"))
                                        ?? $"Training group {legacyId}",
                    TrainingGroupCode = Fit(context, "TrainingGroup", "TrainingGroupCode", Text(reader, "EgitimGrubuKodu")),
                    OrderNo = Int(reader, "Sira"),
                }));
            }
        }

        var written = await SaveAsync(db, context, "EgitimGrubu_T", rows, cancellationToken);
        return new StepResult(read, written, 0, $"training groups: {written} written");
    }

    // ------------------------------------------------------------------ trainings

    private static async Task<StepResult> MigrateTrainingsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var groupMap = await context.IdMap.LoadAsync("EgitimGrubu_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var already = await context.IdMap.LoadAsync("Egitim_T", cancellationToken);

        var rows = new List<(int LegacyId, Training Entity)>();
        var durations = new List<(int LegacyId, HazardClass Hazard, int Minutes)>();
        var read = 0;

        const string sql = """
            SELECT EgitimId, EgitimAdi, EgitimKodu, Sure, GenelKonular, SaglikKonulari, TeknikKonular,
                   ZorunluEgitim, EgitimGrubuId, Aktif, TehlikeliSure, AzTehlikeliSure,
                   CokTehlikeliSure, DefaultPlan, DefaultEgitim, DefaultAdet,
                   DefaultBaslangicAyKaydirma, DefaultElemanSarti, IBYS_EgitimKodu, KurumId
            FROM Egitim_T ORDER BY EgitimId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;
                var legacyId = Required(reader, "EgitimId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                foreach (var (hazard, column) in new[]
                         {
                             (HazardClass.LowHazard, "AzTehlikeliSure"),
                             (HazardClass.Hazardous, "TehlikeliSure"),
                             (HazardClass.VeryHazardous, "CokTehlikeliSure"),
                         })
                {
                    if (Int(reader, column) is > 0 and { } minutes)
                    {
                        durations.Add((legacyId, hazard, minutes));
                    }
                }

                rows.Add((legacyId, new Training
                {
                    TrainingName = Fit(context, "Training", "TrainingName", Text(reader, "EgitimAdi"))
                                   ?? $"Training {legacyId}",
                    TrainingCode = Fit(context, "Training", "TrainingCode", Text(reader, "EgitimKodu")),
                    TrainingGroupId = MapId(groupMap, Int(reader, "EgitimGrubuId")),
                    // As with Aktivite_T.Tur, the legacy Tur column records a staff role, not a
                    // training type. Left at its default rather than guessed.
                    TrainingType = TrainingType.BasicTraining,
                    TopicGroup = MapTopicGroup(reader),
                    MandatoryTraining = Bit(reader, "ZorunluEgitim"),
                    IsActive = Bit(reader, "Aktif"),
                    IbysTrainingCode = Int(reader, "IBYS_EgitimKodu"),
                    IncludedInDefaultPlan = Bit(reader, "DefaultPlan"),
                    DefaultTraining = Bit(reader, "DefaultEgitim"),
                    DefaultCount = Int(reader, "DefaultAdet") ?? 0,
                    DefaultStartMonthOffset = Int(reader, "DefaultBaslangicAyKaydirma") ?? 0,
                    DefaultElementCondition = Int(reader, "DefaultElemanSarti") ?? 0,
                    TenantId = MapId(organizationMap, Int(reader, "KurumId")),
                }));
            }
        }

        var written = await SaveAsync(db, context, "Egitim_T", rows, cancellationToken);

        // The statutory duration differs by hazard class, which is three columns in the legacy table
        // and three rows here.
        var durationCount = 0;
        if (!context.DryRun && durations.Count > 0)
        {
            var map = await context.IdMap.LoadAsync("Egitim_T", cancellationToken);
            await using var durationDb = context.CreateDbContext();

            var entities = durations
                .Where(item => map.ContainsKey(item.LegacyId))
                .Select(item => new TrainingDuration
                {
                    TrainingId = map[item.LegacyId],
                    HazardClass = item.Hazard,
                    DurationMinutes = item.Minutes,
                })
                .ToList();

            durationDb.Set<TrainingDuration>().AddRange(entities);
            await durationDb.SaveChangesAsync(cancellationToken);
            durationCount = entities.Count;
        }

        var note = $"trainings: {written} written";
        if (durationCount > 0)
        {
            note += $", {durationCount} statutory duration(s) by hazard class";
        }

        return new StepResult(read, written, 0, note);
    }

    /// <summary>
    /// The subject group, from the three legacy booleans.
    /// <para>
    /// This is the field <c>CompanyComplianceCalculator</c> uses to tell safety training from health
    /// training, so a wrong value here misreports every company's outstanding obligations. General
    /// is the fallback because that is what the legacy default plan treats an unflagged training as.
    /// </para>
    /// </summary>
    private static TrainingSubjectGroup MapTopicGroup(SqlDataReader reader)
    {
        if (Bit(reader, "SaglikKonulari"))
        {
            return TrainingSubjectGroup.HealthSubjects;
        }

        return Bit(reader, "TeknikKonular")
            ? TrainingSubjectGroup.TechnicalSubjects
            : TrainingSubjectGroup.GeneralSubjects;
    }

    // ------------------------------------------------------------------ shared

    private static async Task<int> SaveAsync<TEntity>(
        DbContext db,
        MigrationContext context,
        string legacyTable,
        List<(int LegacyId, TEntity Entity)> rows,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (rows.Count == 0)
        {
            return 0;
        }

        if (context.DryRun)
        {
            return rows.Count;
        }

        db.Set<TEntity>().AddRange(rows.Select(item => item.Entity));
        await db.SaveChangesAsync(cancellationToken);

        var pairs = rows
            .Select(item => (item.LegacyId, (int)db.Entry(item.Entity).Property("Id").CurrentValue!))
            .ToList();

        await context.IdMap.SaveAsync(legacyTable, pairs, 'I', cancellationToken);
        db.ChangeTracker.Clear();

        return rows.Count;
    }

    private static async Task<int> ApplyLinksAsync(
        MigrationContext context,
        string table,
        string column,
        List<(int Id, int Value)> links,
        CancellationToken cancellationToken)
    {
        if (links.Count == 0)
        {
            return 0;
        }

        var updated = 0;
        await using var connection = await context.OpenModernAsync(cancellationToken);

        foreach (var chunk in links.Chunk(1000))
        {
            var values = string.Join(",", chunk.Select(link => $"({link.Id},{link.Value})"));

            await using var command = new SqlCommand($"""
                UPDATE target SET {column} = source.Value
                FROM {table} AS target
                JOIN (VALUES {values}) AS source (Id, Value) ON target.Id = source.Id;
                """, connection) { CommandTimeout = 600 };

            updated += await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return updated;
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

    private static int Required(SqlDataReader reader, string column)
        => reader.GetInt32(reader.GetOrdinal(column));

    private static int? MapId(Dictionary<int, int> map, int? legacyId)
        => legacyId is { } id && map.TryGetValue(id, out var modernId) ? modernId : null;
}
