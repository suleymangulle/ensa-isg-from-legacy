using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Communication;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The two log tables and the mail queue.
/// <para>
/// <b>The training log moves; the application log does not.</b> They look alike — both are
/// millions of rows written by the legacy application — and they are not alike at all.
/// </para>
/// <para>
/// <c>PersonelLoglamasi_T</c> records what a <i>worker</i> did: signed in, turned to page 14,
/// finished a topic, sat a test. 19,736,085 of its 19,819,018 rows are page turns, and that is the
/// point — the elapsed and remaining seconds on each one are the evidence of how long somebody
/// actually spent on statutory training, which is what an inspection asks to see. The progress
/// records carry the totals; this carries what the totals are made of.
/// </para>
/// <para>
/// <c>Log_T</c> records what the <i>legacy code</i> did: which .cshtml page and which C# method
/// ran, with a parameter dump. Those pages and methods do not exist in the rebuilt system, so
/// 7,722,484 rows of them would be diagnostics for an application nobody can run any more, sitting
/// in the table the new one wants to write its own diagnostics into. It is left behind by default
/// and carried by <c>--include-legacy-log</c> for anybody who disagrees — the decision is one flag
/// away, not one migration away.
/// </para>
/// </summary>
public sealed class LogStep : IMigrationStep
{
    public int Order => 112;

    public string Name => "logs";

    public string Description => "The worker training log (19.8M rows) and the mail queue";

    private const int ChunkSize = 200_000;

    /// <summary>Set from the command line to carry <c>Log_T</c> as well.</summary>
    public bool IncludeApplicationLog { get; init; }

    private static readonly string[] TrainingLogColumns =
    [
        "CompanyEmployeeId", "Operation", "TrainingTopicId", "Page", "ElapsedDurationSeconds",
        "RemainingDurationSeconds", "ExamId", "ExamNote", "EmployeeTrainingProgressId",
        "CreationTime", "TenantId",
    ];

    private static readonly string[] ApplicationLogColumns =
    [
        "LineNo", "PageName", "MethodName", "Message", "UserId", "Parameters", "LogLevel",
        "CreationTime", "TenantId",
    ];

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var results = new List<StepResult>
        {
            await MailsAsync(context, cancellationToken),
            await TrainingLogAsync(context, cancellationToken),
        };

        results.Add(IncludeApplicationLog
            ? await ApplicationLogAsync(context, cancellationToken)
            : new StepResult(0, 0, 0,
                "application log: NOT carried — 7,722,484 rows naming legacy pages and methods that "
                + "no longer exist; pass --include-legacy-log to carry it anyway"));

        return new StepResult(
            results.Sum(r => r.Read),
            results.Sum(r => r.Written),
            results.Sum(r => r.Skipped),
            string.Join("; ", results.Select(r => r.Note).Where(note => note is not null)));
    }

    // ------------------------------------------------------------------ mail queue

    /// <summary>
    /// <c>Mail_T</c> to <see cref="Mail"/> — what the system sent and what it failed to send.
    /// <para>
    /// The legacy table has no organization column, so every mail is written host-level. That is
    /// honest rather than convenient: the queue was a single system-wide one, and inventing a
    /// tenant for each row from the recipient's address would be a guess about who a message
    /// belonged to.
    /// </para>
    /// </summary>
    private static async Task<StepResult> MailsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var already = await context.IdMap.LoadAsync("Mail_T", cancellationToken);

        var read = 0;
        var written = 0;
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, Mail Entity)>();

        const string sql = """
            SELECT MailId, Gonderen, Alici, Konu, MailIcerigi, Icerik_Format, MailOnemi,
                   MailTuru, MailDurumu, HataMesaji
            FROM Mail_T ORDER BY MailId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "MailId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                batch.Add((legacyId, new Mail
                {
                    Sender = Fit(context, "Mail", "Sender", Text(reader, "Gonderen")) ?? string.Empty,
                    Recipient = Fit(context, "Mail", "Recipient", Text(reader, "Alici")) ?? string.Empty,
                    Topic = Fit(context, "Mail", "Topic", Text(reader, "Konu")) ?? string.Empty,
                    Content = Text(reader, "MailIcerigi") ?? string.Empty,
                    ContentFormat = Fold(Text(reader, "Icerik_Format")) == "HTML"
                        ? ContentFormat.Html
                        : ContentFormat.PlainText,
                    MailPriority = PriorityOf(Text(reader, "MailOnemi")),
                    MailType = MailTypeOf(Text(reader, "MailTuru")),
                    MailStatus = StatusOf(Text(reader, "MailDurumu")),
                    ErrorMessage = Fit(context, "Mail", "ErrorMessage", Text(reader, "HataMesaji")),

                    // The legacy table records neither when a mail was sent nor how many times it
                    // was tried. A sent mail with no date is what the source says; a date invented
                    // from the row's position would be a delivery record nobody made.
                    SubmissionDate = null,
                    AttemptCount = 0,

                    CreationTime = DateTime.Now,
                    TenantId = null,
                }));

                if (batch.Count >= 500 && !context.DryRun)
                {
                    written += await FlushAsync(db, context, "Mail_T", batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            if (context.DryRun)
            {
                written += batch.Count;
                batch.Clear();
            }
            else
            {
                written += await FlushAsync(db, context, "Mail_T", batch, pairs, cancellationToken);
            }
        }

        return new StepResult(read, written, read - written, $"mail queue: {written} written");
    }

    // ------------------------------------------------------------------ the training log

    private static async Task<StepResult> TrainingLogAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        await using (var model = context.CreateDbContext())
        {
            BulkWriter.EnsureNoConverters(model, "EmployeeTrainingLog");
        }

        var employeeMap = await context.IdMap.LoadAsync("FirmaPersonel_T", cancellationToken);
        var topicMap = await context.IdMap.LoadAsync("EgitimKonu_T", cancellationToken);
        var examMap = await context.IdMap.LoadAsync("Test_T", cancellationToken);
        var progressMap = await context.IdMap.LoadAsync("PersonelEgitimIlerlemeDurum_T", cancellationToken);
        var employeeTenants = await LoadTenantsAsync(context, "ensa.CompanyEmployee", cancellationToken);

        var watermark = await context.IdMap.GetWatermarkAsync("PersonelLoglamasi_T", cancellationToken);
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
                const string sql = """
                    SELECT TOP (@take) PersonelLogId, PersonelId, Islem, IslemTarihi, IslenenKonu,
                           IslenenSayfa, SinavNotu, GecenSure, KalanSure, TestId,
                           PersonelEgitimIlerlemeDurumId
                    FROM PersonelLoglamasi_T
                    WHERE PersonelLogId > @after
                    ORDER BY PersonelLogId;
                    """;

                await using var connection = await context.OpenLegacyAsync(token);
                await using var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 };
                command.Parameters.AddWithValue("@take", ChunkSize);
                command.Parameters.AddWithValue("@after", after);

                await using var reader = await command.ExecuteReaderAsync(token);

                while (await reader.ReadAsync(token))
                {
                    chunkRead++;
                    lastId = Required(reader, "PersonelLogId");

                    if (!employeeMap.TryGetValue(Required(reader, "PersonelId"), out var employeeId))
                    {
                        chunkOrphaned++;
                        continue;
                    }

                    yield return
                    [
                        employeeId,
                        (int)ActionOf(Int(reader, "Islem")),
                        Lookup(topicMap, Int(reader, "IslenenKonu")),
                        Int(reader, "IslenenSayfa"),
                        Int(reader, "GecenSure"),
                        Int(reader, "KalanSure"),
                        Lookup(examMap, Int(reader, "TestId")),
                        Int(reader, "SinavNotu"),
                        Lookup(progressMap, Int(reader, "PersonelEgitimIlerlemeDurumId")),
                        Date(reader, "IslemTarihi") ?? DateTime.Now,
                        employeeTenants.GetValueOrDefault(employeeId),
                    ];
                }
            }

            int chunkWritten;
            if (context.DryRun)
            {
                await foreach (var _ in RowsAsync(cancellationToken))
                {
                }

                chunkWritten = chunkRead - chunkOrphaned;
            }
            else
            {
                chunkWritten = await context.Bulk.WriteAsync(
                    "ensa.EmployeeTrainingLog", TrainingLogColumns, RowsAsync(cancellationToken), cancellationToken);
            }

            read += chunkRead;
            written += chunkWritten;
            orphaned += chunkOrphaned;

            if (chunkRead == 0)
            {
                break;
            }

            watermark = lastId;

            if (!context.DryRun)
            {
                await context.IdMap.SetWatermarkAsync("PersonelLoglamasi_T", watermark, cancellationToken);
            }

            context.Logger.LogInformation(
                "    training log: {Written} written so far (legacy id {Watermark})", written, watermark);

            if (context.DryRun)
            {
                break;
            }
        }

        var note = $"training log: {written} written";
        if (startedAt > 0)
        {
            note += $" (resumed from legacy id {startedAt})";
        }

        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (employee missing)";
        }

        if (context.DryRun)
        {
            note += " — dry run stops after one chunk";
        }

        return new StepResult(read, written, orphaned, note);
    }

    // ------------------------------------------------------------------ the application log

    private static async Task<StepResult> ApplicationLogAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        await using (var model = context.CreateDbContext())
        {
            BulkWriter.EnsureNoConverters(model, "Log");
        }

        var userMap = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        var watermark = await context.IdMap.GetWatermarkAsync("Log_T", cancellationToken);
        var startedAt = watermark;

        var read = 0;
        var written = 0;

        while (true)
        {
            var chunkRead = 0;
            var lastId = watermark;
            var after = watermark;

            async IAsyncEnumerable<object?[]> RowsAsync(
                [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
            {
                const string sql = """
                    SELECT TOP (@take) LogId, Row, PageName, MethodName, Message, Tarih,
                           KullaniciId, Parameters, KurumId, LogType
                    FROM Log_T WHERE LogId > @after ORDER BY LogId;
                    """;

                await using var connection = await context.OpenLegacyAsync(token);
                await using var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 };
                command.Parameters.AddWithValue("@take", ChunkSize);
                command.Parameters.AddWithValue("@after", after);

                await using var reader = await command.ExecuteReaderAsync(token);

                while (await reader.ReadAsync(token))
                {
                    chunkRead++;
                    lastId = Required(reader, "LogId");

                    yield return
                    [
                        Int(reader, "Row"),
                        Fit(context, "Log", "PageName", Text(reader, "PageName")) ?? string.Empty,
                        Fit(context, "Log", "MethodName", Text(reader, "MethodName")) ?? string.Empty,
                        Text(reader, "Message") ?? string.Empty,
                        Lookup(userMap, Int(reader, "KullaniciId")),
                        Text(reader, "Parameters"),

                        // LogType is a nullable bit: true is an error, false a trace, and null a
                        // row written before the column existed. Nothing distinguishes a warning.
                        (int)(Bool(reader, "LogType") == true
                            ? Ensa.Domain.Shared.Enums.LogLevel.Error
                            : Ensa.Domain.Shared.Enums.LogLevel.Info),

                        Date(reader, "Tarih") ?? DateTime.Now,
                        Lookup(organizationMap, Int(reader, "KurumId")),
                    ];
                }
            }

            int chunkWritten;
            if (context.DryRun)
            {
                await foreach (var _ in RowsAsync(cancellationToken))
                {
                }

                chunkWritten = chunkRead;
            }
            else
            {
                chunkWritten = await context.Bulk.WriteAsync(
                    "ensa.Log", ApplicationLogColumns, RowsAsync(cancellationToken), cancellationToken);
            }

            read += chunkRead;
            written += chunkWritten;

            if (chunkRead == 0)
            {
                break;
            }

            watermark = lastId;

            if (!context.DryRun)
            {
                await context.IdMap.SetWatermarkAsync("Log_T", watermark, cancellationToken);
            }

            context.Logger.LogInformation(
                "    application log: {Written} written so far (legacy id {Watermark})", written, watermark);

            if (context.DryRun)
            {
                break;
            }
        }

        var note = $"application log: {written} written";
        if (startedAt > 0)
        {
            note += $" (resumed from legacy id {startedAt})";
        }

        return new StepResult(read, written, 0, note);
    }

    // ------------------------------------------------------------------ value mapping

    /// <summary>
    /// <c>PersonelLoglamasi_T.Islem</c>. The legacy numbers and
    /// <see cref="EmployeeTrainingAction"/> agree one for one, which is not a coincidence — the
    /// enum was written from this column. Values 1, 2, 3, 4, 6, 7 and 10 occur; 5, 8 and 9 do not.
    /// </summary>
    private static EmployeeTrainingAction ActionOf(int? action)
        => action is >= 1 and <= 10
            ? (EmployeeTrainingAction)action.Value
            : EmployeeTrainingAction.TopicProcessing;

    private static MailStatus StatusOf(string? status)
        => Fold(status) switch
        {
            "GONDERILDI" => MailStatus.Sent,
            "GONDERILEMEDI" => MailStatus.Failed,
            "TASLAK" => MailStatus.Draft,
            "IPTAL" => MailStatus.Cancelled,
            _ => MailStatus.Queued,
        };

    private static MailPriority PriorityOf(string? priority)
        => Fold(priority) switch
        {
            "YUKSEK" or "HIGH" => MailPriority.High,
            "DUSUK" or "LOW" => MailPriority.Low,
            _ => MailPriority.Normal,
        };

    private static MailType MailTypeOf(string? type)
        => Fold(type) switch
        {
            "FARKINDALIK" => MailType.Awareness,
            "HATIRLATMA" => MailType.Reminder,
            "SISTEM" => MailType.System,
            _ => MailType.Normal,
        };

    private static string? Fold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var folded = value.Trim()
            .Replace('ı', 'i').Replace('İ', 'I')
            .Replace('ş', 's').Replace('Ş', 'S')
            .Replace('ğ', 'g').Replace('Ğ', 'G')
            .Replace('ü', 'u').Replace('Ü', 'U')
            .Replace('ö', 'o').Replace('Ö', 'O')
            .Replace('ç', 'c').Replace('Ç', 'C')
            .ToUpperInvariant();

        return folded.Length == 0 ? null : folded;
    }

    // ------------------------------------------------------------------ helpers

    private static async Task<Dictionary<int, int?>> LoadTenantsAsync(
        MigrationContext context,
        string modernTable,
        CancellationToken cancellationToken)
    {
        var tenants = new Dictionary<int, int?>();

        await using var connection = await context.OpenModernAsync(cancellationToken);
        await using var command = new SqlCommand($"SELECT Id, TenantId FROM {modernTable};", connection)
        {
            CommandTimeout = 900,
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tenants[reader.GetInt32(0)] = reader.IsDBNull(1) ? null : reader.GetInt32(1);
        }

        return tenants;
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

    private static int? Lookup(Dictionary<int, int> map, int? legacyId)
        => legacyId is int id && map.TryGetValue(id, out var modernId) ? modernId : null;

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

    private static bool? Bool(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : Convert.ToBoolean(reader.GetValue(index));
    }

    private static DateTime? Date(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }

    private static int Required(SqlDataReader reader, string column)
        => reader.GetInt32(reader.GetOrdinal(column));
}
