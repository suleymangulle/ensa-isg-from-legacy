using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Companies;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Trainings;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The e-learning side of training: the topics, the tests, and what each worker answered.
/// <para>
/// A worker takes a first test, reads the topics, then takes a final test, and the record of that
/// is what proves the training happened: 6,500 progress records and 160,121 answers. The tests
/// themselves are small — three of them, thirty-four questions, four answers each — because they
/// are the ministry's standard OHS test rather than something each organization writes.
/// </para>
/// <para>
/// <b>The correct answer is a letter, not a row.</b> <c>Sorular_T.DogruCevap</c> holds "A" to "D"
/// and <c>Cevaplar_T</c> holds four unlabelled rows per question. The letter is therefore the
/// answer's position, which is only safe because every one of the thirty-four questions has
/// exactly four answers — checked, and the step refuses a question that does not.
/// </para>
/// </summary>
public sealed class TrainingExamStep : IMigrationStep
{
    public int Order => 104;

    public string Name => "training-exams";

    public string Description => "Training topics, tests, and 160,121 worker answers";

    private const int BatchSize = 500;

    /// <summary>The three hazard classes a topic has a separate statutory duration for.</summary>
    private static readonly (string Column, HazardClass Class)[] Durations =
    [
        ("AzTehlikeliSure", HazardClass.LowHazard),
        ("TehlikeliSure", HazardClass.Hazardous),
        ("CokTehlikeliSure", HazardClass.VeryHazardous),
    ];

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var results = new List<StepResult>
        {
            await ExamsAsync(context, cancellationToken),
            await QuestionsAsync(context, cancellationToken),
            await AnswersAsync(context, cancellationToken),
            await TrainingExamsAsync(context, cancellationToken),
            await TopicsAsync(context, cancellationToken),
            await TopicDurationsAsync(context, cancellationToken),
            await ProgressAsync(context, cancellationToken),
            await EmployeeAnswersAsync(context, cancellationToken),
            await ProgressModesAsync(context, cancellationToken),
        };

        return new StepResult(
            results.Sum(r => r.Read),
            results.Sum(r => r.Written),
            results.Sum(r => r.Skipped),
            string.Join("; ", results.Select(r => r.Note).Where(note => note is not null)));
    }

    // ------------------------------------------------------------------ the tests

    private static async Task<StepResult> ExamsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<Exam>(
            context, "Test_T", "exams",
            "SELECT TestId, Baslik, Aktif, Silindi, KurumId, Ekleme_Tarihi, Guncelleme_Tarihi FROM Test_T ORDER BY TestId;",
            "TestId",
            (reader, _) => new Exam
            {
                Title = Fit(context, "Exam", "Title", Text(reader, "Baslik")) ?? string.Empty,
                IsActive = Bit(reader, "Aktif"),
                IsDeleted = Bit(reader, "Silindi"),
                CreationTime = Date(reader, "Ekleme_Tarihi") ?? DateTime.Now,
                LastModificationTime = Date(reader, "Guncelleme_Tarihi"),
                TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
            },
            cancellationToken);
    }

    private static async Task<StepResult> QuestionsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var examMap = await context.IdMap.LoadAsync("Test_T", cancellationToken);
        var examTenants = await LoadTenantsAsync(context, "ensa.Exam", cancellationToken);

        return await CopyAsync<ExamQuestion>(
            context, "Sorular_T", "exam questions",
            "SELECT SoruId, TestId, Metin, DogruCevap, Aktif, Silindi, Ekleme_Tarihi FROM Sorular_T ORDER BY SoruId;",
            "SoruId",
            (reader, orphan) =>
            {
                if (!examMap.TryGetValue(Required(reader, "TestId"), out var examId))
                {
                    orphan();
                    return null;
                }

                return new ExamQuestion
                {
                    ExamId = examId,
                    Text = Fit(context, "ExamQuestion", "Text", Text(reader, "Metin")) ?? string.Empty,
                    CorrectAnswer = Fit(context, "ExamQuestion", "CorrectAnswer", Text(reader, "DogruCevap")),
                    IsActive = Bit(reader, "Aktif"),
                    IsDeleted = Bit(reader, "Silindi"),
                    CreationTime = Date(reader, "Ekleme_Tarihi") ?? DateTime.Now,
                    TenantId = examTenants.TryGetValue(examId, out var tenantId) ? tenantId : null,
                };
            },
            cancellationToken);
    }

    /// <summary>
    /// <c>Cevaplar_T</c> to <see cref="ExamAnswer"/>, with the letter worked out from position.
    /// <para>
    /// The legacy answers carry no label; the question names its correct answer as "A" to "D", so
    /// the label is the answer's place in its question, read in id order. That holds only while
    /// every question has exactly four answers, so a question that does not is refused rather than
    /// mislabelled — an exam whose correct answer moved is worse than an exam that did not migrate.
    /// </para>
    /// </summary>
    private static async Task<StepResult> AnswersAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var questionMap = await context.IdMap.LoadAsync("Sorular_T", cancellationToken);
        if (questionMap.Count == 0)
        {
            return new StepResult(0, 0, 0, "exam answers: nothing to do, no question is mapped");
        }

        var correctByQuestion = new Dictionary<int, string?>();
        var questionTenants = new Dictionary<int, int?>();

        await using (var db = context.CreateDbContext())
        {
            foreach (var row in await db.Set<ExamQuestion>()
                         .Select(q => new { q.Id, q.CorrectAnswer, q.TenantId })
                         .ToListAsync(cancellationToken))
            {
                correctByQuestion[row.Id] = row.CorrectAnswer;
                questionTenants[row.Id] = row.TenantId;
            }
        }

        // How many answers each legacy question has, so a question that is not a four-option
        // question can be refused rather than guessed at.
        var counts = new Dictionary<int, int>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
                         "SELECT SoruId, COUNT(*) FROM Cevaplar_T GROUP BY SoruId;", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                counts[reader.GetInt32(0)] = reader.GetInt32(1);
            }
        }

        var refused = counts.Values.Count(count => count != 4);
        var ordinals = new Dictionary<int, int>();

        var result = await CopyAsync<ExamAnswer>(
            context, "Cevaplar_T", "exam answers",
            "SELECT CevapId, SoruId, Cevap_Metni, Silindi FROM Cevaplar_T ORDER BY SoruId, CevapId;",
            "CevapId",
            (reader, orphan) =>
            {
                var legacyQuestionId = Required(reader, "SoruId");

                if (!questionMap.TryGetValue(legacyQuestionId, out var questionId)
                    || counts.GetValueOrDefault(legacyQuestionId) != 4)
                {
                    orphan();
                    return null;
                }

                ordinals.TryGetValue(legacyQuestionId, out var ordinal);
                ordinals[legacyQuestionId] = ordinal + 1;

                var letter = ((char)('A' + ordinal)).ToString();

                return new ExamAnswer
                {
                    ExamQuestionId = questionId,
                    AnswerText = Fit(context, "ExamAnswer", "AnswerText", Text(reader, "Cevap_Metni")) ?? string.Empty,
                    IsCorrect = string.Equals(correctByQuestion.GetValueOrDefault(questionId), letter, StringComparison.OrdinalIgnoreCase),
                    IsDeleted = Bit(reader, "Silindi"),
                    CreationTime = DateTime.Now,
                    TenantId = questionTenants.GetValueOrDefault(questionId),
                };
            },
            cancellationToken);

        var note = result.Note;
        if (refused > 0)
        {
            note += $"; {refused} question(s) REFUSED, they do not have exactly four answers";
        }

        return result with { Note = note };
    }

    private static async Task<StepResult> TrainingExamsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var trainingMap = await context.IdMap.LoadAsync("Egitim_T", cancellationToken);
        var examMap = await context.IdMap.LoadAsync("Test_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<TrainingExam>(
            context, "KonuTest_T", "training exams",
            "SELECT KonuTestId, EgitimId, TestId, Aktif, Silindi, KurumId FROM KonuTest_T ORDER BY KonuTestId;",
            "KonuTestId",
            (reader, orphan) =>
            {
                if (!trainingMap.TryGetValue(Required(reader, "EgitimId"), out var trainingId)
                    || !examMap.TryGetValue(Required(reader, "TestId"), out var examId))
                {
                    orphan();
                    return null;
                }

                return new TrainingExam
                {
                    TrainingId = trainingId,
                    ExamId = examId,
                    IsActive = Bit(reader, "Aktif"),
                    IsDeleted = Bit(reader, "Silindi"),
                    CreationTime = DateTime.Now,
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ topics

    private static async Task<StepResult> TopicsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var trainingMap = await context.IdMap.LoadAsync("Egitim_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<TrainingTopic>(
            context, "EgitimKonu_T", "training topics",
            """
            SELECT KonuId, EgitimId, KonuBasligi, SunumAdresi, SunumSayfaSayisi, KonuSirasi,
                   Silindi, KurumId, EklemeTarihi, GuncellenmeTarihi
            FROM EgitimKonu_T ORDER BY KonuId;
            """,
            "KonuId",
            (reader, orphan) =>
            {
                if (!trainingMap.TryGetValue(Required(reader, "EgitimId"), out var trainingId))
                {
                    orphan();
                    return null;
                }

                return new TrainingTopic
                {
                    TrainingId = trainingId,
                    TopicTitle = Fit(context, "TrainingTopic", "TopicTitle", Text(reader, "KonuBasligi")) ?? string.Empty,
                    PresentationAddress = Fit(context, "TrainingTopic", "PresentationAddress", Text(reader, "SunumAdresi")),
                    PresentationPageCount = Int(reader, "SunumSayfaSayisi") ?? 0,
                    TopicOrder = Int(reader, "KonuSirasi") ?? 0,
                    IsDeleted = Bit(reader, "Silindi"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellenmeTarihi"),
                    TenantId = Int(reader, "KurumId") is int legacyTenantId
                               && organizationMap.TryGetValue(legacyTenantId, out var tenantId)
                        ? tenantId
                        : null,
                };
            },
            cancellationToken);
    }

    /// <summary>
    /// The three statutory durations a topic has, one per hazard class, which the legacy table
    /// keeps in three columns of the topic itself.
    /// <para>
    /// A duration of zero is written like any other: the regulation says a low-hazard workplace
    /// owes no minutes for some topics, and that is a fact, not a missing value.
    /// </para>
    /// </summary>
    private static async Task<StepResult> TopicDurationsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var topicMap = await context.IdMap.LoadAsync("EgitimKonu_T", cancellationToken);
        if (topicMap.Count == 0)
        {
            return new StepResult(0, 0, 0, null);
        }

        var done = (await db.Set<TrainingTopicDuration>()
            .Select(d => d.TrainingTopicId).Distinct().ToListAsync(cancellationToken)).ToHashSet();

        var topicTenants = await LoadTenantsAsync(context, "ensa.TrainingTopic", cancellationToken);

        var read = 0;
        var batch = new List<TrainingTopicDuration>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
                         "SELECT KonuId, AzTehlikeliSure, TehlikeliSure, CokTehlikeliSure FROM EgitimKonu_T ORDER BY KonuId;",
                         connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                if (!topicMap.TryGetValue(Required(reader, "KonuId"), out var topicId) || done.Contains(topicId))
                {
                    continue;
                }

                foreach (var (column, hazardClass) in Durations)
                {
                    batch.Add(new TrainingTopicDuration
                    {
                        TrainingTopicId = topicId,
                        HazardClass = hazardClass,
                        DurationMinutes = Int(reader, column) ?? 0,
                        CreationTime = DateTime.Now,
                        TenantId = topicTenants.GetValueOrDefault(topicId),
                    });
                }
            }
        }

        if (!context.DryRun && batch.Count > 0)
        {
            foreach (var chunk in batch.Chunk(BatchSize))
            {
                db.AddRange(chunk);
                await db.SaveChangesAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }
        }

        return new StepResult(read, batch.Count, 0, $"topic durations: {batch.Count} written");
    }

    // ------------------------------------------------------------------ what each worker did

    private static async Task<StepResult> ProgressAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var employeeMap = await context.IdMap.LoadAsync("FirmaPersonel_T", cancellationToken);
        var trainingMap = await context.IdMap.LoadAsync("Egitim_T", cancellationToken);
        var topicMap = await context.IdMap.LoadAsync("EgitimKonu_T", cancellationToken);
        var userMap = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var employeeTenants = await LoadTenantsAsync(context, "ensa.CompanyEmployee", cancellationToken);

        return await CopyAsync<EmployeeTrainingProgress>(
            context, "PersonelEgitimIlerlemeDurum_T", "training progress",
            """
            SELECT IlerlemeDurumId, PersonelId, EgitimId, KonuId, IlkTestDurum, IlkTestNotu,
                   SonTestDurum, SonTestNotu, GecenSure, AktifSayfa,
                   EgitimUzmanId, EgitimHekimId, Aktif
            FROM PersonelEgitimIlerlemeDurum_T ORDER BY IlerlemeDurumId;
            """,
            "IlerlemeDurumId",
            (reader, orphan) =>
            {
                if (!employeeMap.TryGetValue(Required(reader, "PersonelId"), out var employeeId)
                    || !trainingMap.TryGetValue(Required(reader, "EgitimId"), out var trainingId))
                {
                    orphan();
                    return null;
                }

                return new EmployeeTrainingProgress
                {
                    CompanyEmployeeId = employeeId,
                    TrainingId = trainingId,
                    TrainingTopicId = Lookup(topicMap, Int(reader, "KonuId")),
                    FirstTestCompleted = Bit(reader, "IlkTestDurum"),
                    FirstTestNote = Int(reader, "IlkTestNotu"),
                    LatestTestCompleted = Bit(reader, "SonTestDurum"),
                    LatestTestNote = Int(reader, "SonTestNotu"),
                    ElapsedDurationSeconds = Int(reader, "GecenSure") ?? 0,
                    ActivePage = Int(reader, "AktifSayfa") ?? 0,
                    TrainingSpecialistUserId = Lookup(userMap, Int(reader, "EgitimUzmanId")),
                    TrainingPhysicianUserId = Lookup(userMap, Int(reader, "EgitimHekimId")),
                    IsActive = Bit(reader, "Aktif"),
                    CreationTime = DateTime.Now,
                    TenantId = employeeTenants.GetValueOrDefault(employeeId),
                };
            },
            cancellationToken);
    }

    /// <summary>
    /// <c>PersonelSoruCevap_T</c> to <see cref="EmployeeExamAnswer"/>: 160,121 answers.
    /// <para>
    /// <c>TestTip</c> is 1 for the first test and 2 for the final one — the legacy enum has only
    /// those two members and the legacy code writes only those two values. 160 rows hold 3, which
    /// nothing in the application produces. They are counted and left behind: an answer filed
    /// under the wrong attempt says a worker passed a test they did not sit, and there is nothing
    /// in the source that says which attempt a 3 belongs to.
    /// </para>
    /// </summary>
    private static async Task<StepResult> EmployeeAnswersAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var employeeMap = await context.IdMap.LoadAsync("FirmaPersonel_T", cancellationToken);
        var questionMap = await context.IdMap.LoadAsync("Sorular_T", cancellationToken);
        var progressMap = await context.IdMap.LoadAsync("PersonelEgitimIlerlemeDurum_T", cancellationToken);
        var employeeTenants = await LoadTenantsAsync(context, "ensa.CompanyEmployee", cancellationToken);

        var watermark = await context.IdMap.GetWatermarkAsync("PersonelSoruCevap_T", cancellationToken);
        var startedAt = watermark;

        var read = 0;
        var written = 0;
        var orphaned = 0;
        var unknownAttempt = 0;
        var batch = new List<EmployeeExamAnswer>();

        const string sql = """
            SELECT PersonelSoruCevapId, FirmaPersonelId, SoruId, Cevap, Durum,
                   CevaplanmaTarihi, IlerlemeDurumId, TestTip
            FROM PersonelSoruCevap_T
            WHERE PersonelSoruCevapId > @after
            ORDER BY PersonelSoruCevapId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 })
        {
            command.Parameters.AddWithValue("@after", watermark);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                read++;
                var legacyId = Required(reader, "PersonelSoruCevapId");

                var attempt = Int(reader, "TestTip");
                if (attempt is not (1 or 2))
                {
                    unknownAttempt++;
                    watermark = legacyId;
                    continue;
                }

                if (!employeeMap.TryGetValue(Required(reader, "FirmaPersonelId"), out var employeeId)
                    || !questionMap.TryGetValue(Required(reader, "SoruId"), out var questionId)
                    || !progressMap.TryGetValue(Required(reader, "IlerlemeDurumId"), out var progressId))
                {
                    orphaned++;
                    watermark = legacyId;
                    continue;
                }

                batch.Add(new EmployeeExamAnswer
                {
                    CompanyEmployeeId = employeeId,
                    ExamQuestionId = questionId,
                    Answer = Fit(context, "EmployeeExamAnswer", "Answer", Text(reader, "Cevap")),

                    // Durum is the legacy application's own verdict on the answer, written when
                    // the worker submitted it. Recomputing it from the question's correct letter
                    // would silently re-mark tests somebody has already been certified on.
                    IsCorrect = Bit(reader, "Durum"),

                    EmployeeTrainingProgressId = progressId,
                    TestType = attempt == 2 ? ExamAttemptType.FinalTest : ExamAttemptType.FirstTest,
                    CevaplanmaDate = Date(reader, "CevaplanmaTarihi") ?? DateTime.Now,
                    CreationTime = Date(reader, "CevaplanmaTarihi") ?? DateTime.Now,
                    TenantId = employeeTenants.GetValueOrDefault(employeeId),
                });

                watermark = legacyId;

                if (batch.Count >= 2_000 && !context.DryRun)
                {
                    db.AddRange(batch);
                    await db.SaveChangesAsync(cancellationToken);
                    written += batch.Count;
                    batch.Clear();
                    db.ChangeTracker.Clear();

                    await context.IdMap.SetWatermarkAsync("PersonelSoruCevap_T", watermark, cancellationToken);

                    context.Logger.LogInformation("    worker answers: {Written} written so far", written);
                }
            }
        }

        if (batch.Count > 0)
        {
            if (!context.DryRun)
            {
                db.AddRange(batch);
                await db.SaveChangesAsync(cancellationToken);
                await context.IdMap.SetWatermarkAsync("PersonelSoruCevap_T", watermark, cancellationToken);
            }

            written += batch.Count;
        }

        var note = $"worker answers: {written} written";
        if (startedAt > 0)
        {
            note += $" (resumed from legacy id {startedAt})";
        }

        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (employee, question or progress record missing)";
        }

        if (unknownAttempt > 0)
        {
            note += $", {unknownAttempt} SKIPPED (TestTip is neither first nor final test)";
        }

        return new StepResult(read, written, orphaned + unknownAttempt, note);
    }

    private static async Task<StepResult> ProgressModesAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var userMap = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var companyTenants = await LoadTenantsAsync(context, "ensa.Company", cancellationToken);

        return await CopyAsync<CompanyTrainingProgressMode>(
            context, "FirmaEgitimGecis_T", "training progress modes",
            "SELECT GecisId, FirmaId, ManuelGecis, KullaniciId, Tarih FROM FirmaEgitimGecis_T ORDER BY GecisId;",
            "GecisId",
            (reader, orphan) =>
            {
                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !userMap.TryGetValue(Required(reader, "KullaniciId"), out var userId))
                {
                    orphan();
                    return null;
                }

                return new CompanyTrainingProgressMode
                {
                    CompanyId = companyId,
                    TransitionMode = Fold(Text(reader, "ManuelGecis")) == "SAYFA"
                        ? TrainingProgressMode.Page
                        : TrainingProgressMode.Topic,
                    UserId = userId,
                    Date = Date(reader, "Tarih") ?? DateTime.Now,
                    CreationTime = Date(reader, "Tarih") ?? DateTime.Now,
                    TenantId = companyTenants.GetValueOrDefault(companyId),
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ the shared copy

    private static async Task<StepResult> CopyAsync<TEntity>(
        MigrationContext context,
        string legacyTable,
        string label,
        string sql,
        string keyColumn,
        Func<SqlDataReader, Action, TEntity?> project,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        await using var db = context.CreateDbContext();

        var already = await context.IdMap.LoadAsync(legacyTable, cancellationToken);

        var read = 0;
        var written = 0;
        var orphaned = 0;
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, TEntity Entity)>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, keyColumn);
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                var wasOrphaned = false;
                var entity = project(reader, () => wasOrphaned = true);

                if (entity is null || wasOrphaned)
                {
                    orphaned++;
                    continue;
                }

                batch.Add((legacyId, entity));

                if (batch.Count >= BatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, legacyTable, batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, legacyTable, batch, pairs, cancellationToken);
        }

        var note = $"{label}: {written} written";
        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (a referenced record is missing)";
        }

        return new StepResult(read, written, orphaned, note);
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

    private static string? Fold(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().Replace('ı', 'i').Replace('İ', 'I').ToUpperInvariant();

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

    private static DateTime? Date(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }

    private static bool Bit(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return !reader.IsDBNull(index) && Convert.ToBoolean(reader.GetValue(index));
    }

    private static int Required(SqlDataReader reader, string column)
        => reader.GetInt32(reader.GetOrdinal(column));
}
