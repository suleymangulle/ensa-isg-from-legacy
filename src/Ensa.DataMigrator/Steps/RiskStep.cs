using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Risks;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// Risk assessment reports and the million hazards they identify.
/// <para>
/// The hazard catalogue first (147 categories, 3,615 hazards), then 6,844 reports through the
/// DbContext, then 1,000,579 identified hazards in bulk.
/// </para>
/// <para>
/// <b>The risk score is computed here, because the rebuilt schema stores it.</b> The legacy system
/// recomputed it on every screen and kept no column; <c>IdentifiedHazard.RiskScore</c> is
/// persistent, for reporting and sorting. A migration that left it at zero would leave every report
/// sorted by nothing and every risk looking negligible — so it is calculated the same way
/// <c>IRiskAssessmentManager</c> does: likelihood × severity for a matrix, likelihood × frequency ×
/// severity for Fine-Kinney.
/// </para>
/// </summary>
public sealed class RiskStep : IMigrationStep
{
    public int Order => 70;

    public string Name => "risks";

    public string Description => "Hazard catalogue, risk assessment reports and their million identified hazards";

    private const int ParentBatchSize = 500;
    private const int LineChunkSize = 200_000;

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        await using (var model = context.CreateDbContext())
        {
            BulkWriter.EnsureNoConverters(model, "IdentifiedHazard");
        }

        var read = 0;
        var written = 0;
        var skipped = 0;
        var notes = new List<string>();

        var categories = await MigrateHazardCategoriesAsync(context, cancellationToken);
        var hazards = await MigrateHazardsAsync(context, categories.Map, cancellationToken);
        var reports = await MigrateReportsAsync(context, cancellationToken);
        var identified = await MigrateIdentifiedHazardsAsync(
            context, reports.Map, categories.Map, hazards.Map, reports.Methods, cancellationToken);

        foreach (var result in new[] { categories.Result, hazards.Result, reports.Result, identified })
        {
            read += result.Read;
            written += result.Written;
            skipped += result.Skipped;
            notes.Add(result.Note!);
        }

        return new StepResult(read, written, skipped, string.Join("; ", notes));
    }

    // ------------------------------------------------------------------ hazard catalogue

    private static async Task<(StepResult Result, Dictionary<int, int> Map)> MigrateHazardCategoriesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();
        var already = await context.IdMap.LoadAsync("TehlikeKategori_T", cancellationToken);

        var rows = new List<(int LegacyId, HazardCategory Entity)>();
        var read = 0;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT TehlikeKategoriId, KategoriAdi, SiraNo, TehlikeKaynagi, DataType FROM TehlikeKategori_T ORDER BY TehlikeKategoriId",
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;
                var legacyId = Required(reader, "TehlikeKategoriId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                rows.Add((legacyId, new HazardCategory
                {
                    CategoryName = Fit(context, "HazardCategory", "CategoryName", Text(reader, "KategoriAdi"))
                                   ?? $"Category {legacyId}",
                    SortOrder = Int(reader, "SiraNo") ?? 0,
                    IsHazardSource = Bit(reader, "TehlikeKaynagi"),
                    DataType = Fit(context, "HazardCategory", "DataType", Text(reader, "DataType")),
                    IsActive = true,
                }));
            }
        }

        var written = await SaveAsync(db, context, "TehlikeKategori_T", rows, cancellationToken);
        var map = await MapAfterSaveAsync(context, "TehlikeKategori_T", already, rows, cancellationToken);

        return (new StepResult(read, written, 0, $"hazard categories: {written} written"), map);
    }

    private static async Task<(StepResult Result, Dictionary<int, int> Map)> MigrateHazardsAsync(
        MigrationContext context,
        Dictionary<int, int> categoryMap,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();
        var already = await context.IdMap.LoadAsync("Tehlike_T", cancellationToken);

        var rows = new List<(int LegacyId, Hazard Entity)>();
        var read = 0;
        var orphaned = 0;

        const string sql = """
            SELECT TehlikeId, TehlikeKategoriId, Tehlike, Risk, Onlem, Olasilik, Siddet, Frekans,
                   DefaultTehlike, Aktif
            FROM Tehlike_T ORDER BY TehlikeId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;
                var legacyId = Required(reader, "TehlikeId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                if (!categoryMap.TryGetValue(Required(reader, "TehlikeKategoriId"), out var categoryId))
                {
                    orphaned++;
                    continue;
                }

                rows.Add((legacyId, new Hazard
                {
                    HazardCategoryId = categoryId,
                    HazardTag = Fit(context, "Hazard", "HazardTag", Text(reader, "Tehlike")) ?? $"Hazard {legacyId}",
                    RiskTag = Fit(context, "Hazard", "RiskTag", Text(reader, "Risk")),
                    Measure = Fit(context, "Hazard", "Measure", Text(reader, "Onlem")),
                    Likelihood = Number(reader, "Olasilik") ?? 0,
                    Severity = Number(reader, "Siddet") ?? 0,
                    Frequency = Number(reader, "Frekans") ?? 0,
                    IsDefault = Bit(reader, "DefaultTehlike"),
                    IsActive = Bit(reader, "Aktif"),
                }));
            }
        }

        var written = await SaveAsync(db, context, "Tehlike_T", rows, cancellationToken);
        var map = await MapAfterSaveAsync(context, "Tehlike_T", already, rows, cancellationToken);

        var note = $"hazards: {written} written";
        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (category missing)";
        }

        return (new StepResult(read, written, orphaned, note), map);
    }

    // ------------------------------------------------------------------ reports

    private static async Task<(StepResult Result, Dictionary<int, int> Map, Dictionary<int, RiskAssessmentMethod> Methods)>
        MigrateReportsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var already = await context.IdMap.LoadAsync("RiskAnalizRaporu_T", cancellationToken);

        var read = 0;
        var written = 0;
        var orphaned = 0;
        var pairs = new List<(int, int)>();
        var methods = new Dictionary<int, RiskAssessmentMethod>();
        var batch = new List<(int LegacyId, RiskAssessmentReport Entity)>();

        const string sql = """
            SELECT RiskAnalizRaporuId, RaporAdi, FirmaId, IsyeriUnvani, FaaliyetAlani, IsyeriAdresi,
                   IsyeriTelefonu, TehlikeSinifi, IsyeriBolumleri, MakinelerVeEkipmanlar,
                   TehlikeliMaddeler, AtikIslemleri, GerceklestirmeTarihi, GecerlilikTarihi,
                   RevizeTarihi, Isveren, Uzman, Doktor, CalisanSayisi, RaporMetodu, KayitDurumu,
                   EklemeTarihi, KurumId, SilindiMi
            FROM RiskAnalizRaporu_T ORDER BY RiskAnalizRaporuId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "RiskAnalizRaporuId");
                var method = MapMethod(Text(reader, "RaporMetodu"));
                methods[legacyId] = method;

                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId))
                {
                    orphaned++;
                    continue;
                }

                batch.Add((legacyId, new RiskAssessmentReport
                {
                    ReportName = Fit(context, "RiskAssessmentReport", "ReportName", Text(reader, "RaporAdi"))
                                 ?? $"Report {legacyId}",
                    CompanyId = companyId,
                    WorkplaceTitle = Fit(context, "RiskAssessmentReport", "WorkplaceTitle", Text(reader, "IsyeriUnvani"))
                                     ?? string.Empty,
                    BusinessActivity = Fit(context, "RiskAssessmentReport", "BusinessActivity", Text(reader, "FaaliyetAlani"))
                                       ?? string.Empty,
                    WorkplaceAddress = Fit(context, "RiskAssessmentReport", "WorkplaceAddress", Text(reader, "IsyeriAdresi"))
                                       ?? string.Empty,
                    WorkplaceTelefonu = Fit(context, "RiskAssessmentReport", "WorkplaceTelefonu", Text(reader, "IsyeriTelefonu"))
                                        ?? string.Empty,
                    HazardClass = MapHazardClass(Text(reader, "TehlikeSinifi")),
                    WorkplaceDepartments = Fit(context, "RiskAssessmentReport", "WorkplaceDepartments", Text(reader, "IsyeriBolumleri")),
                    MachinesVeEquipments = Fit(context, "RiskAssessmentReport", "MachinesVeEquipments", Text(reader, "MakinelerVeEkipmanlar")),
                    HazardousArticles = Fit(context, "RiskAssessmentReport", "HazardousArticles", Text(reader, "TehlikeliMaddeler")),
                    WasteOperations = Fit(context, "RiskAssessmentReport", "WasteOperations", Text(reader, "AtikIslemleri")),
                    PerformedDate = Date(reader, "GerceklestirmeTarihi") ?? DateTime.Now,
                    ValidityDate = Date(reader, "GecerlilikTarihi") ?? DateTime.Now,
                    RevisionDate = Date(reader, "RevizeTarihi"),
                    Employer = Fit(context, "RiskAssessmentReport", "Employer", Text(reader, "Isveren")),
                    // The legacy columns hold the names as typed, not references to a user. Kept as
                    // names; inventing a link by matching text would attach reports to the wrong
                    // people wherever two specialists share a name.
                    SpecialistFullName = Fit(context, "RiskAssessmentReport", "SpecialistFullName", Text(reader, "Uzman")),
                    PhysicianFullName = Fit(context, "RiskAssessmentReport", "PhysicianFullName", Text(reader, "Doktor")),
                    WorkerCount = Int(reader, "CalisanSayisi") ?? 0,
                    ReportMethod = method,
                    ApprovalStatus = string.Equals(Text(reader, "KayitDurumu"), "Tamamlandi", StringComparison.OrdinalIgnoreCase)
                        ? ApprovalStatus.Approved
                        : ApprovalStatus.Draft,
                    TenantId = MapId(organizationMap, Int(reader, "KurumId")),
                    IsDeleted = Bit(reader, "SilindiMi"),
                }));

                if (batch.Count >= ParentBatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, "RiskAnalizRaporu_T", batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, "RiskAnalizRaporu_T", batch, pairs, cancellationToken);
        }

        var note = $"risk reports: {written} written";
        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (company missing)";
        }

        var map = new Dictionary<int, int>(already);
        foreach (var (legacyId, modernId) in pairs)
        {
            map[legacyId] = modernId;
        }

        return (new StepResult(read, written, orphaned, note), map, methods);
    }

    // ------------------------------------------------------------------ identified hazards

    private static async Task<StepResult> MigrateIdentifiedHazardsAsync(
        MigrationContext context,
        Dictionary<int, int> reportMap,
        Dictionary<int, int> categoryMap,
        Dictionary<int, int> hazardMap,
        Dictionary<int, RiskAssessmentMethod> methods,
        CancellationToken cancellationToken)
    {
        string[] columns =
        [
            "RiskAssessmentReportId", "HazardCategoryId", "HazardId", "HazardTag",
            "ActivityDescription", "OwnerPerson", "RiskTag", "Measure",
            "Likelihood", "Severity", "Frequency", "RiskScore", "Comment",
            "ResidualLikelihood", "ResidualSeverity", "ResidualFrequency", "ResidualRiskScore",
            "ResidualComment", "SourceType", "SourceId", "DeadlineDate",
            "CreationTime", "IsDeleted", "TenantId",
        ];

        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        const string sql = """
            SELECT TOP (@take) RiskAnalizRaporuBelirlenenTehlikeId, RiskAnalizRaporuId,
                   TehlikeKategoriId, TehlikeId, Tehlike, Faaliyet, SorumluKisi, Risk, Onlem,
                   Olasilik, Siddet, Frekans, Yorum, TSOlasilik, TSSiddet, TSFrekans, TSYorum,
                   KaynakId, TerminTarihi, KurumId
            FROM RiskAnalizRaporuBelirlenenTehlike_T
            WHERE RiskAnalizRaporuBelirlenenTehlikeId > @after
            ORDER BY RiskAnalizRaporuBelirlenenTehlikeId;
            """;

        var watermark = await context.IdMap.GetWatermarkAsync(
            "RiskAnalizRaporuBelirlenenTehlike_T", cancellationToken);

        var read = 0;
        var written = 0;
        var orphaned = 0;

        while (true)
        {
            var chunkRead = 0;
            var chunkOrphaned = 0;
            var lastId = watermark;
            var after = watermark;

            async IAsyncEnumerable<object?[]> RowsAsync(
                [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
            {
                await using var connection = await context.OpenLegacyAsync(token);
                await using var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 };
                command.Parameters.AddWithValue("@take", LineChunkSize);
                command.Parameters.AddWithValue("@after", after);

                await using var reader = await command.ExecuteReaderAsync(token);

                while (await reader.ReadAsync(token))
                {
                    chunkRead++;
                    lastId = Required(reader, "RiskAnalizRaporuBelirlenenTehlikeId");

                    var legacyReportId = Required(reader, "RiskAnalizRaporuId");
                    if (!reportMap.TryGetValue(legacyReportId, out var reportId))
                    {
                        chunkOrphaned++;
                        continue;
                    }

                    var method = methods.GetValueOrDefault(legacyReportId, RiskAssessmentMethod.FineKinney);

                    var likelihood = Number(reader, "Olasilik") ?? 0;
                    var severity = Number(reader, "Siddet") ?? 0;
                    var frequency = Number(reader, "Frekans") ?? 0;

                    var residualLikelihood = Number(reader, "TSOlasilik");
                    var residualSeverity = Number(reader, "TSSiddet");
                    var residualFrequency = Number(reader, "TSFrekans");

                    var hazardId = MapId(hazardMap, Int(reader, "TehlikeId"));

                    yield return
                    [
                        reportId,
                        MapId(categoryMap, Int(reader, "TehlikeKategoriId")),
                        hazardId,
                        Fit(context, "IdentifiedHazard", "HazardTag", Text(reader, "Tehlike")) ?? string.Empty,
                        Fit(context, "IdentifiedHazard", "ActivityDescription", Text(reader, "Faaliyet")),
                        Fit(context, "IdentifiedHazard", "OwnerPerson", Text(reader, "SorumluKisi")),
                        Fit(context, "IdentifiedHazard", "RiskTag", Text(reader, "Risk")),
                        Fit(context, "IdentifiedHazard", "Measure", Text(reader, "Onlem")),
                        likelihood,
                        severity,
                        frequency,
                        Score(method, likelihood, severity, frequency),
                        Fit(context, "IdentifiedHazard", "Comment", Text(reader, "Yorum")),
                        residualLikelihood,
                        residualSeverity,
                        residualFrequency,
                        residualLikelihood is null || residualSeverity is null
                            ? null
                            : Score(method, residualLikelihood.Value, residualSeverity.Value,
                                    residualFrequency ?? 0),
                        Fit(context, "IdentifiedHazard", "ResidualComment", Text(reader, "TSYorum")),
                        // A hazard taken from the catalogue names it; one typed in does not.
                        (int)(hazardId is null ? HazardSourceType.Manual : HazardSourceType.HazardLibrary),
                        Int(reader, "KaynakId"),
                        Date(reader, "TerminTarihi"),
                        DateTime.Now,
                        false,
                        MapId(organizationMap, Int(reader, "KurumId")),
                    ];
                }
            }

            if (context.DryRun)
            {
                await foreach (var _ in RowsAsync(cancellationToken))
                {
                }

                read += chunkRead;
                written += chunkRead - chunkOrphaned;
                orphaned += chunkOrphaned;
                break;
            }

            var chunkWritten = await context.Bulk.WriteAsync(
                "ensa.IdentifiedHazard", columns, RowsAsync(cancellationToken), cancellationToken);

            read += chunkRead;
            written += chunkWritten;
            orphaned += chunkOrphaned;

            if (chunkRead == 0)
            {
                break;
            }

            watermark = lastId;
            await context.IdMap.SetWatermarkAsync(
                "RiskAnalizRaporuBelirlenenTehlike_T", watermark, cancellationToken);

            context.Logger.LogInformation(
                "    identified hazards: {Written} written so far (legacy id {Watermark})", written, watermark);
        }

        var note = $"identified hazards: {written} written";
        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (report missing)";
        }

        if (context.DryRun)
        {
            note += " — dry run stops after one chunk";
        }

        return new StepResult(read, written, orphaned, note);
    }

    /// <summary>
    /// The risk score, computed the way <c>IRiskAssessmentManager</c> computes it.
    /// <para>
    /// The rebuilt schema persists this; the legacy system recomputed it on every screen and stored
    /// nothing. Leaving it at zero would make every migrated risk look negligible and every report
    /// sort by nothing.
    /// </para>
    /// </summary>
    private static decimal Score(
        RiskAssessmentMethod method, decimal likelihood, decimal severity, decimal frequency)
        => method == RiskAssessmentMethod.FineKinney
            ? likelihood * frequency * severity
            : likelihood * severity;

    /// <summary>
    /// The assessment method, from the legacy free-text column.
    /// <para>
    /// <c>"matris"</c> becomes the five-by-five matrix rather than the three-by-three, decided from
    /// the data rather than the name: 427,460 of 427,488 rows in matrix reports have a likelihood of
    /// five or less, and 267,856 of three or less. A 3×3 matrix would not produce the fours and
    /// fives; a 5×5 produces exactly this.
    /// </para>
    /// </summary>
    private static RiskAssessmentMethod MapMethod(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "finekinney" => RiskAssessmentMethod.FineKinney,
        "matris" => RiskAssessmentMethod.LMatrixFiveByFive,
        _ => RiskAssessmentMethod.Unspecified,
    };

    private static HazardClass MapHazardClass(string? value)
    {
        var folded = value?.Trim()
            .Replace('İ', 'i').Replace('I', 'ı')
            .ToLowerInvariant()
            .Replace('ı', 'i').Replace('ç', 'c').Replace('ö', 'o').Replace('ü', 'u');

        if (folded is null)
        {
            return HazardClass.Unspecified;
        }

        if (folded.StartsWith("cok tehlikeli", StringComparison.Ordinal)) return HazardClass.VeryHazardous;
        if (folded.StartsWith("az tehlikeli", StringComparison.Ordinal)) return HazardClass.LowHazard;
        if (folded.StartsWith("tehlikeli", StringComparison.Ordinal)) return HazardClass.Hazardous;

        return HazardClass.Unspecified;
    }

    // ------------------------------------------------------------------ shared

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

    private static async Task<int> SaveAsync<TEntity>(
        DbContext db,
        MigrationContext context,
        string legacyTable,
        List<(int LegacyId, TEntity Entity)> rows,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (rows.Count == 0 || context.DryRun)
        {
            return rows.Count;
        }

        foreach (var chunk in rows.Chunk(ParentBatchSize))
        {
            db.Set<TEntity>().AddRange(chunk.Select(item => item.Entity));
            await db.SaveChangesAsync(cancellationToken);

            await context.IdMap.SaveAsync(
                legacyTable,
                chunk.Select(item => (item.LegacyId, (int)db.Entry(item.Entity).Property("Id").CurrentValue!)).ToList(),
                'I', cancellationToken);

            db.ChangeTracker.Clear();
        }

        return rows.Count;
    }

    /// <summary>The map after a save, including a dry run's placeholders.</summary>
    private static async Task<Dictionary<int, int>> MapAfterSaveAsync<TEntity>(
        MigrationContext context,
        string legacyTable,
        Dictionary<int, int> already,
        List<(int LegacyId, TEntity Entity)> rows,
        CancellationToken cancellationToken)
    {
        if (context.DryRun)
        {
            var placeholder = new Dictionary<int, int>(already);
            foreach (var (legacyId, _) in rows)
            {
                placeholder[legacyId] = context.NextDryRunId();
            }

            return placeholder;
        }

        return await context.IdMap.LoadAsync(legacyTable, cancellationToken);
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

    /// <summary>
    /// A risk factor, kept inside what the column can hold.
    /// <para>
    /// The legacy columns are floats with nothing enforcing a scale; the destination is a decimal
    /// with a fixed precision, and one absurd value would stop a chunk of two hundred thousand rows.
    /// </para>
    /// </summary>
    private static decimal? Number(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = Convert.ToDouble(reader.GetValue(index));
        return value is >= 0 and <= 10_000 ? Math.Round((decimal)value, 2) : null;
    }

    private static int Required(SqlDataReader reader, string column)
        => reader.GetInt32(reader.GetOrdinal(column));

    private static int? MapId(Dictionary<int, int> map, int? legacyId)
        => legacyId is { } id && map.TryGetValue(id, out var modernId) ? modernId : null;
}
