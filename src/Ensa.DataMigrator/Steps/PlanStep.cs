using Ensa.DataMigrator.Infrastructure;
using Ensa.EntityFrameworkCore.ValueConverters;
using Ensa.Domain.Plans;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Trainings;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The annual work and training plans, and their two million lines.
/// <para>
/// A plan is the year's commitment to a workplace; each line is one activity or training in one
/// month. The plans themselves are small — 22,902 and 19,305 — and their lines are not: 1,047,164
/// and 965,904.
/// </para>
/// <para>
/// <b>Split by what each half needs.</b> A plan is a parent, so it goes through the DbContext and
/// records an id map its lines then resolve against. A line is a leaf that nothing points at, and
/// there are two million of them, so it goes through <see cref="BulkWriter"/> with a watermark.
/// Both halves of that rule are checked rather than assumed — <c>EnsureNoConverters</c> would
/// refuse a line table that ever gained an encrypted column.
/// </para>
/// <para>
/// <b>Approval state is carried, not recomputed.</b> A line that was approved in the legacy system
/// stays approved, with the same approver and date. Resetting everything to draft would be tidier
/// and would erase a year of somebody's sign-offs.
/// </para>
/// </summary>
public sealed class PlanStep : IMigrationStep
{
    public int Order => 60;

    public string Name => "plans";

    public string Description => "Work and training plans, and their two million lines";

    private const int ParentBatchSize = 500;
    private const int LineChunkSize = 200_000;

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        await using (var model = context.CreateDbContext())
        {
            // The instructor's identity number is encrypted on both line tables. It is converted by
            // hand below - see Encrypt - so it is named here; anything else encrypted still stops
            // the run.
            BulkWriter.EnsureNoConverters(model, "WorkPlanLine", ["InstructorNationalId"]);
            BulkWriter.EnsureNoConverters(model, "TrainingPlanLine", ["InstructorNationalId"]);
        }

        var read = 0;
        var written = 0;
        var skipped = 0;
        var notes = new List<string>();

        // Each parent hands its map to its lines. A dry run persists nothing, so a line stage that
        // re-read the map from the database would find it empty and report every line an orphan.
        var workPlans = await MigrateWorkPlansAsync(context, cancellationToken);
        var workPlanLines = await MigrateWorkPlanLinesAsync(context, workPlans.Map, cancellationToken);
        var trainingPlans = await MigrateTrainingPlansAsync(context, cancellationToken);
        var trainingPlanLines = await MigrateTrainingPlanLinesAsync(context, trainingPlans.Map, cancellationToken);

        foreach (var result in new[]
                 {
                     workPlans.Result, workPlanLines, trainingPlans.Result, trainingPlanLines,
                 })
        {
            read += result.Read;
            written += result.Written;
            skipped += result.Skipped;
            notes.Add(result.Note!);
        }

        return new StepResult(read, written, skipped, string.Join("; ", notes));
    }

    // ------------------------------------------------------------------ work plans

    private static async Task<(StepResult Result, Dictionary<int, int> Map)> MigrateWorkPlansAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var userMap = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var already = await context.IdMap.LoadAsync("CalismaPlani_T", cancellationToken);

        var read = 0;
        var written = 0;
        var orphaned = 0;
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, WorkPlan Entity)>();

        const string sql = """
            SELECT CalismaPlaniId, FirmaId, BaslangicTarihi, RevizyonNo, RevizyonTarihi, DokumanNo,
                   YayinTarihi, UzmanId, DoktorId, OnaylayanKullaniciId, Aktif, Aktarildi, KurumId
            FROM CalismaPlani_T ORDER BY CalismaPlaniId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "CalismaPlaniId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId))
                {
                    orphaned++;
                    continue;
                }

                batch.Add((legacyId, new WorkPlan
                {
                    CompanyId = companyId,
                    StartDate = Date(reader, "BaslangicTarihi") ?? DateTime.Now,
                    RevisionNo = Fit(context, "WorkPlan", "RevisionNo", Text(reader, "RevizyonNo")),
                    RevisionDate = Date(reader, "RevizyonTarihi") ?? DateTime.Now,
                    DocumentNo = Fit(context, "WorkPlan", "DocumentNo", Text(reader, "DokumanNo")),
                    PublicationDate = Date(reader, "YayinTarihi") ?? DateTime.Now,
                    SpecialistUserId = MapId(userMap, Int(reader, "UzmanId")),
                    PhysicianUserId = MapId(userMap, Int(reader, "DoktorId")),
                    ApproverUserId = MapId(userMap, Int(reader, "OnaylayanKullaniciId")),
                    IsActive = Bit(reader, "Aktif"),
                    IsTransferred = Bit(reader, "Aktarildi"),
                    TenantId = MapId(organizationMap, Int(reader, "KurumId")),
                }));

                if (batch.Count >= ParentBatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, "CalismaPlani_T", batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, "CalismaPlani_T", batch, pairs, cancellationToken);
        }

        var note = $"work plans: {written} written";
        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (company missing)";
        }

        var map = new Dictionary<int, int>(already);
        foreach (var (legacyId, modernId) in pairs)
        {
            map[legacyId] = modernId;
        }

        return (new StepResult(read, written, orphaned, note), map);
    }

    // ------------------------------------------------------------------ work plan lines

    private static async Task<StepResult> MigrateWorkPlanLinesAsync(
        MigrationContext context,
        Dictionary<int, int> planMap,
        CancellationToken cancellationToken)
    {
        string[] columns =
        [
            "WorkPlanId", "ActivityId", "PeriodId", "Year", "Month", "Status", "PerformedDate",
            "Description", "IsActive", "ApprovalStatus", "ForApprovalSenderUserId", "ApproverUserId",
            "ForApprovalSendingDate", "ApprovalDate", "CompanyId", "InstructorNationalId",
            "CreationTime", "IsDeleted", "TenantId",
        ];

        var activityMap = await context.IdMap.LoadAsync("Aktivite_T", cancellationToken);
        var periodMap = await context.IdMap.LoadAsync("Periyot_T", cancellationToken);
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var userMap = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        const string sql = """
            SELECT TOP (@take) CalismaPlaniSatirId, CalismaPlaniId, AktiviteId, PeriyotId, Yil, Ay,
                   Durum, YapilmaTarihi, Aciklama, Aktif, OnayDurumu, OnayaGonderenId, OnaylayanId,
                   OnayaGondermeTarihi, OnaylanmaTarihi, FirmaId, EgiticiTcKimlikNo,
                   EklemeTarihi, KurumId
            FROM CalismaPlaniSatirlari_T
            WHERE CalismaPlaniSatirId > @after
            ORDER BY CalismaPlaniSatirId;
            """;

        return await CopyLinesAsync(
            context, "CalismaPlaniSatirlari_T", "ensa.WorkPlanLine", columns, sql, "CalismaPlaniSatirId",
            (reader, orphan) =>
            {
                if (!planMap.TryGetValue(Required(reader, "CalismaPlaniId"), out var planId)
                    || !activityMap.TryGetValue(Required(reader, "AktiviteId"), out var activityId)
                    || !companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId))
                {
                    orphan();
                    return null;
                }

                return
                [
                    planId,
                    activityId,
                    MapId(periodMap, Int(reader, "PeriyotId")),
                    Int(reader, "Yil") ?? 0,
                    Int(reader, "Ay"),
                    LineStatus(reader),
                    Date(reader, "YapilmaTarihi"),
                    Fit(context, "WorkPlanLine", "Description", Text(reader, "Aciklama")),
                    Bit(reader, "Aktif"),
                    Enum(reader, "OnayDurumu", 0, 3),
                    MapId(userMap, Int(reader, "OnayaGonderenId")),
                    MapId(userMap, Int(reader, "OnaylayanId")),
                    Date(reader, "OnayaGondermeTarihi"),
                    Date(reader, "OnaylanmaTarihi"),
                    companyId,
                    Encrypt(Fit(context, "WorkPlanLine", "InstructorNationalId",
                        LegacyCrypt.TryDecrypt(Text(reader, "EgiticiTcKimlikNo")))),
                    Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    false,
                    MapId(organizationMap, Int(reader, "KurumId")),
                ];
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ training plans

    private static async Task<(StepResult Result, Dictionary<int, int> Map)> MigrateTrainingPlansAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var userMap = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var already = await context.IdMap.LoadAsync("EgitimPlani_T", cancellationToken);

        var read = 0;
        var written = 0;
        var orphaned = 0;
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, TrainingPlan Entity)>();

        const string sql = """
            SELECT EgitimPlaniId, FirmaId, BaslangicTarihi, RevizyonNo, RevizyonTarihi, DokumanNo,
                   YayinTarihi, UzmanId, DoktorId, OnaylayanKullaniciId, Aktif, Aktarildi, KurumId
            FROM EgitimPlani_T ORDER BY EgitimPlaniId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "EgitimPlaniId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId))
                {
                    orphaned++;
                    continue;
                }

                batch.Add((legacyId, new TrainingPlan
                {
                    CompanyId = companyId,
                    StartDate = Date(reader, "BaslangicTarihi") ?? DateTime.Now,
                    RevisionNo = Fit(context, "TrainingPlan", "RevisionNo", Text(reader, "RevizyonNo")),
                    RevisionDate = Date(reader, "RevizyonTarihi") ?? DateTime.Now,
                    DocumentNo = Fit(context, "TrainingPlan", "DocumentNo", Text(reader, "DokumanNo")),
                    PublicationDate = Date(reader, "YayinTarihi") ?? DateTime.Now,
                    SpecialistUserId = MapId(userMap, Int(reader, "UzmanId")),
                    PhysicianUserId = MapId(userMap, Int(reader, "DoktorId")),
                    ApproverUserId = MapId(userMap, Int(reader, "OnaylayanKullaniciId")),
                    IsActive = Bit(reader, "Aktif"),
                    IsTransferred = Bit(reader, "Aktarildi"),
                    TenantId = MapId(organizationMap, Int(reader, "KurumId")),
                }));

                if (batch.Count >= ParentBatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, "EgitimPlani_T", batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, "EgitimPlani_T", batch, pairs, cancellationToken);
        }

        var note = $"training plans: {written} written";
        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (company missing)";
        }

        var map = new Dictionary<int, int>(already);
        foreach (var (legacyId, modernId) in pairs)
        {
            map[legacyId] = modernId;
        }

        return (new StepResult(read, written, orphaned, note), map);
    }

    // ------------------------------------------------------------------ training plan lines

    private static async Task<StepResult> MigrateTrainingPlanLinesAsync(
        MigrationContext context,
        Dictionary<int, int> planMap,
        CancellationToken cancellationToken)
    {
        string[] columns =
        [
            "TrainingPlanId", "TrainingId", "CompanyId", "DurationMinutes", "Year", "Month",
            "Status", "ApprovalStatus", "PerformedDate", "Source", "Description", "IsActive",
            "ForApprovalSenderUserId", "ApproverUserId", "ForApprovalSendingDate", "ApprovalDate",
            "InstructorNationalId", "InstructorTitle", "InstructorFullName", "TrainingLocation",
            "TrainingType", "IbysStatus", "IbysStatusCode", "IbysMessage",
            "CreationTime", "IsDeleted", "TenantId",
        ];

        var trainingMap = await context.IdMap.LoadAsync("Egitim_T", cancellationToken);
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var userMap = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        const string sql = """
            SELECT TOP (@take) EgitimPlaniSatirId, EgitimPlaniId, EgitimId, FirmaId, Sure, Yil, Ay,
                   Durum, OnayDurumu, YapilmaTarihi, Kaynak, Aciklama, Aktif, OnayaGonderenId,
                   OnaylayanId, OnayaGondermeTarihi, OnaylanmaTarihi, EgiticiTcKimlikNo,
                   EgiticiUnvan, EgiticiAdSoyad, EgitimYeri, EgitimTuru,
                   IbysDurumKodu, IbysMessage, EklemeTarihi, KurumId
            FROM EgitimPlaniSatirlari_T
            WHERE EgitimPlaniSatirId > @after
            ORDER BY EgitimPlaniSatirId;
            """;

        return await CopyLinesAsync(
            context, "EgitimPlaniSatirlari_T", "ensa.TrainingPlanLine", columns, sql, "EgitimPlaniSatirId",
            (reader, orphan) =>
            {
                if (!planMap.TryGetValue(Required(reader, "EgitimPlaniId"), out var planId)
                    || !trainingMap.TryGetValue(Required(reader, "EgitimId"), out var trainingId))
                {
                    orphan();
                    return null;
                }

                return
                [
                    planId,
                    trainingId,
                    MapId(companyMap, Int(reader, "FirmaId")),
                    Int(reader, "Sure") ?? 0,
                    Int(reader, "Yil"),
                    Int(reader, "Ay"),
                    LineStatus(reader) ?? (int)PlanLineStatus.Planned,
                    Enum(reader, "OnayDurumu", 0, 3),
                    Date(reader, "YapilmaTarihi"),
                    Fit(context, "TrainingPlanLine", "Source", Text(reader, "Kaynak")),
                    Fit(context, "TrainingPlanLine", "Description", Text(reader, "Aciklama")),
                    Bit(reader, "Aktif"),
                    MapId(userMap, Int(reader, "OnayaGonderenId")),
                    MapId(userMap, Int(reader, "OnaylayanId")),
                    Date(reader, "OnayaGondermeTarihi"),
                    Date(reader, "OnaylanmaTarihi"),
                    Encrypt(Fit(context, "TrainingPlanLine", "InstructorNationalId",
                        LegacyCrypt.TryDecrypt(Text(reader, "EgiticiTcKimlikNo")))),
                    Fit(context, "TrainingPlanLine", "InstructorTitle", Text(reader, "EgiticiUnvan")),
                    Fit(context, "TrainingPlanLine", "InstructorFullName", Text(reader, "EgiticiAdSoyad")),
                    Enum(reader, "EgitimYeri", 1, 3),
                    Enum(reader, "EgitimTuru", 1, 3),
                    // IBYS submission state is not carried: the legacy status is a free-text code
                    // from a service that has since changed, and claiming a notification was
                    // accepted when nobody can check is worse than starting from not-sent.
                    (int)IbysSubmissionStatus.NotSent,
                    Fit(context, "TrainingPlanLine", "IbysStatusCode", Text(reader, "IbysDurumKodu")),
                    Fit(context, "TrainingPlanLine", "IbysMessage", Text(reader, "IbysMessage")),
                    Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    false,
                    MapId(organizationMap, Int(reader, "KurumId")),
                ];
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ the line loop

    /// <summary>
    /// Reads a line table in chunks and bulk-copies each chunk, moving a watermark as it goes.
    /// <para>
    /// Shared by both line tables because they differ only in their columns and their projection —
    /// and because a second copy of a resumable chunked loop is a second place for the watermark to
    /// be moved at the wrong moment.
    /// </para>
    /// </summary>
    private static async Task<StepResult> CopyLinesAsync(
        MigrationContext context,
        string legacyTable,
        string modernTable,
        string[] columns,
        string sql,
        string keyColumn,
        Func<SqlDataReader, Action, object?[]?> project,
        CancellationToken cancellationToken)
    {
        var watermark = await context.IdMap.GetWatermarkAsync(legacyTable, cancellationToken);
        var startedAt = watermark;

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
                    lastId = Required(reader, keyColumn);

                    var row = project(reader, () => chunkOrphaned++);
                    if (row is not null)
                    {
                        yield return row;
                    }
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
                modernTable, columns, RowsAsync(cancellationToken), cancellationToken);

            read += chunkRead;
            written += chunkWritten;
            orphaned += chunkOrphaned;

            if (chunkRead == 0)
            {
                break;
            }

            watermark = lastId;
            await context.IdMap.SetWatermarkAsync(legacyTable, watermark, cancellationToken);

            context.Logger.LogInformation(
                "    {Table}: {Written} written so far (legacy id {Watermark})",
                legacyTable, written, watermark);
        }

        var note = $"{legacyTable}: {written} written";
        if (startedAt > 0)
        {
            note += $" (resumed from {startedAt})";
        }

        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (plan, activity or company missing)";
        }

        if (context.DryRun)
        {
            note += " — dry run stops after one chunk";
        }

        return new StepResult(read, written, orphaned, note);
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
    /// The line's status, including the legacy value that is not in the enum's range.
    /// <para>
    /// <c>-1</c> is not a stray number: <c>ISGDokumantasyon</c> writes it when the document attached
    /// to a line is deleted, so the line reverts from done to not done. Discarding it as
    /// out-of-range threw away the state of 159,405 work plan lines. Read from the legacy source
    /// rather than guessed; anything else still becomes null instead of being forced into the
    /// nearest enum member.
    /// </para>
    /// </summary>
    private static int? LineStatus(SqlDataReader reader)
        => Int(reader, "Durum") switch
        {
            -1 => (int)PlanLineStatus.NotDone,
            0 => (int)PlanLineStatus.Planned,
            1 => (int)PlanLineStatus.Completed,
            2 => (int)PlanLineStatus.NotDone,
            3 => (int)PlanLineStatus.Postponed,
            4 => (int)PlanLineStatus.Cancelled,
            _ => null,
        };

    /// <summary>
    /// An integer that is meant to be an enum value, kept inside the range the enum defines.
    /// <para>
    /// The legacy columns are plain <c>int</c> with nothing enforcing them. A value outside the
    /// range is not a status the rebuilt system can act on, and storing it would produce a record
    /// that renders as a number and behaves like nothing.
    /// </para>
    /// </summary>
    private static int? Enum(SqlDataReader reader, string column, int minimum, int maximum)
    {
        var value = Int(reader, column);
        return value is { } number && number >= minimum && number <= maximum ? number : null;
    }

    /// <summary>
    /// Encrypts a value the way the model would, for a column written in bulk.
    /// <para>
    /// Bulk copy does not run the converters, so a column that needs one has to be converted here.
    /// The same converter and the same process-wide key the application uses — anything else and the
    /// value is unreadable, which is a mistake this migration has already made once at the scale of
    /// a quarter of a million rows.
    /// </para>
    /// </summary>
    private static string? Encrypt(string? plaintext)
        => plaintext is null ? null : (string?)Converter.ConvertToProvider(plaintext);

    private static readonly EncryptedStringConverter Converter = new();

    private static int Required(SqlDataReader reader, string column)
        => reader.GetInt32(reader.GetOrdinal(column));

    private static int? MapId(Dictionary<int, int> map, int? legacyId)
        => legacyId is { } id && map.TryGetValue(id, out var modernId) ? modernId : null;
}
