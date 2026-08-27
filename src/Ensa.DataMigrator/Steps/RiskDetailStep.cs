using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Companies;
using Ensa.Domain.Risks;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The five risk assessment child tables, and the employees' immunisation record.
/// <para>
/// <c>RiskAnalizRaporu_T</c> answers four checklist questions by adding a column per option —
/// ten "who is exposed" bits, seven "what controls exist" bits, seven "what should improve" bits
/// and four "who is vulnerable" bits. The rebuilt schema keeps each as rows, and the enums were
/// written from these columns: ten options against ten members, seven against seven, seven against
/// seven. Nothing here is a judgement call, which is the point — the data was always rows and the
/// legacy schema was the only thing pretending otherwise.
/// </para>
/// <para>
/// The participants are the same shape read the other way: five name columns and one for the
/// workers who know the job, each one a role the report has to record.
/// </para>
/// </summary>
public sealed class RiskDetailStep : IMigrationStep
{
    public int Order => 114;

    public string Name => "risk-detail";

    public string Description => "Exposed groups, control measures, improvement actions, participants and employee immunisations";

    private const int BatchSize = 2_000;

    private static readonly (string Column, ExposedPersonGroup Group)[] ExposedGroups =
    [
        ("TMKIderiPersonel", ExposedPersonGroup.ProductionEmployee),
        ("TMKBakimPersoneli", ExposedPersonGroup.MaintenanceEmployee),
        ("TMKYukleniciler", ExposedPersonGroup.Contractors),
        ("TMKTeknikPersonel", ExposedPersonGroup.TechnicalEmployee),
        ("TMKBuroPersoneli", ExposedPersonGroup.OfficeStaff),
        ("TMKDenetimPersoneli", ExposedPersonGroup.AuditEmployee),
        ("TMKZiyaretciler", ExposedPersonGroup.Visitors),
        ("TMKTemizlemePersoneli", ExposedPersonGroup.CleaningEmployee),
        ("TMKAcilDurumPersoneli", ExposedPersonGroup.EmergencyEmployee),
        ("TMKDigerleri", ExposedPersonGroup.Others),
    ];

    private static readonly (string Column, ExistingControlMeasure Measure)[] ControlMeasures =
    [
        ("MKOLokalHavalandirma", ExistingControlMeasure.LocalVentilation),
        ("MKOMakinaKoruyuculari", ExistingControlMeasure.MachineGuards),
        ("MKOKisiselKoruyucularinKulllanimi", ExistingControlMeasure.PersonalProtectiveUsage),
        ("MKOYanginaKarsiKorunma", ExistingControlMeasure.FireProtection),
        ("MKOMevcutAcilDurumSurecleri", ExistingControlMeasure.EmergencyProcedures),
        ("MKOEgitimVeBilgilendirme", ExistingControlMeasure.TrainingAndAwareness),
        ("MKOUyariLevhalari", ExistingControlMeasure.WarningSigns),
    ];

    private static readonly (string Column, ImprovementAction Action)[] ImprovementActions =
    [
        ("IORiskleriKaynagindaYokEtmek", ImprovementAction.EliminateAtSource),
        ("IOTehlikeliOlaniDahaAzOlanlaDegistirmek", ImprovementAction.SubstituteWithLessHazardous),
        ("IOTopluKorumaOnlemleriniKisiselOlanaTercihErmek", ImprovementAction.PreferCollectiveProtection),
        ("IOMuhendislikOnlemleriniUygulamak", ImprovementAction.ApplyEngineeringControls),
        ("IOErgonomikYaklasimlardanYararlanmak", ImprovementAction.UseErgonomicApproaches),
        ("IOEgitimVeBilgilendirme", ImprovementAction.TrainingAndAwareness),
        ("IOUyariBilgilendirmeVeYonlendirmeLevhalari", ImprovementAction.WarningAndGuidanceSigns),
    ];

    /// <summary>
    /// The four vulnerable groups the legacy report flags. It has no column for a pregnant or
    /// nursing worker, so that member of the enum simply never occurs — an absence in the source,
    /// not a mapping that failed.
    /// </summary>
    private static readonly (string Column, VulnerableWorkerGroup Group)[] ProtectedGroups =
    [
        ("KadinCalisan", VulnerableWorkerGroup.FemaleWorker),
        ("YasliCalisan", VulnerableWorkerGroup.ElderlyWorker),
        ("CocukCalisan", VulnerableWorkerGroup.ChildWorker),
        ("EngelliCalisan", VulnerableWorkerGroup.DisabledWorker),
    ];

    private static readonly (string Column, ReportParticipantType Type)[] Participants =
    [
        ("Isveren", ReportParticipantType.Employer),
        ("Uzman", ReportParticipantType.OccupationalSafetySpecialist),
        ("Doktor", ReportParticipantType.WorkplacePhysician),
        ("IsyeriCalisanTemsilcisi", ReportParticipantType.WorkerRepresentative),
        ("DestekElemani", ReportParticipantType.SupportStaff),
        ("BilgiSahibiCalisanlar", ReportParticipantType.KnowledgeableWorker),
    ];

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var report = await ReportDetailAsync(context, cancellationToken);
        var immunisations = await EmployeeImmunisationsAsync(context, cancellationToken);

        return new StepResult(
            report.Read + immunisations.Read,
            report.Written + immunisations.Written,
            report.Skipped + immunisations.Skipped,
            string.Join("; ", new[] { report.Note, immunisations.Note }.Where(note => note is not null)));
    }

    // ------------------------------------------------------------------ the report's checklists

    /// <summary>
    /// One pass over the 6,844 reports produces all five child tables, because they all come from
    /// the same row and reading it five times to keep the code tidy would be a poor trade.
    /// </summary>
    private static async Task<StepResult> ReportDetailAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var reportMap = await context.IdMap.LoadAsync("RiskAnalizRaporu_T", cancellationToken);
        if (reportMap.Count == 0)
        {
            return new StepResult(0, 0, 0, "risk report detail: nothing to do, no report is mapped");
        }

        // No id map of their own — they are columns, not rows — so a re-run is guarded by which
        // reports already have children. All five tables are asked, because a report can produce
        // rows in one and none in another.
        var done = (await db.Set<RiskAssessmentExposedGroup>()
                .Select(x => x.RiskAssessmentReportId).Distinct().ToListAsync(cancellationToken))
            .Concat(await db.Set<RiskAssessmentControlMeasure>()
                .Select(x => x.RiskAssessmentReportId).Distinct().ToListAsync(cancellationToken))
            .Concat(await db.Set<RiskAssessmentImprovementAction>()
                .Select(x => x.RiskAssessmentReportId).Distinct().ToListAsync(cancellationToken))
            .Concat(await db.Set<RiskAssessmentProtectedGroup>()
                .Select(x => x.RiskAssessmentReportId).Distinct().ToListAsync(cancellationToken))
            .Concat(await db.Set<RiskAssessmentParticipant>()
                .Select(x => x.RiskAssessmentReportId).Distinct().ToListAsync(cancellationToken))
            .ToHashSet();

        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        var exposed = new List<RiskAssessmentExposedGroup>();
        var measures = new List<RiskAssessmentControlMeasure>();
        var actions = new List<RiskAssessmentImprovementAction>();
        var protectedGroups = new List<RiskAssessmentProtectedGroup>();
        var participants = new List<RiskAssessmentParticipant>();

        var read = 0;
        var written = 0;

        var columns = new List<string> { "RiskAnalizRaporuId", "KurumId", "EklemeTarihi" };
        columns.AddRange(ExposedGroups.Select(g => g.Column));
        columns.AddRange(ControlMeasures.Select(m => m.Column));
        columns.AddRange(ImprovementActions.Select(a => a.Column));
        columns.AddRange(ProtectedGroups.Select(g => g.Column));
        columns.AddRange(Participants.Select(p => p.Column));

        var sql = $"""
            SELECT {string.Join(", ", columns.Select(c => $"[{c}]"))}
            FROM RiskAnalizRaporu_T ORDER BY RiskAnalizRaporuId;
            """;

        async Task FlushAsync()
        {
            var count = exposed.Count + measures.Count + actions.Count
                        + protectedGroups.Count + participants.Count;

            if (!context.DryRun)
            {
                db.AddRange(exposed);
                db.AddRange(measures);
                db.AddRange(actions);
                db.AddRange(protectedGroups);
                db.AddRange(participants);
                await db.SaveChangesAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }

            written += count;

            exposed.Clear();
            measures.Clear();
            actions.Clear();
            protectedGroups.Clear();
            participants.Clear();
        }

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                if (!reportMap.TryGetValue(Required(reader, "RiskAnalizRaporuId"), out var reportId)
                    || done.Contains(reportId))
                {
                    continue;
                }

                var tenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenant)
                    ? tenant
                    : (int?)null;
                var created = Date(reader, "EklemeTarihi") ?? DateTime.Now;

                // Only the boxes that are ticked. An unticked box says the group is not exposed or
                // the control is not in place, and writing a row for it would turn "no" into a
                // record of a measure the workplace does not have.
                foreach (var (column, group) in ExposedGroups)
                {
                    if (Bit(reader, column))
                    {
                        exposed.Add(new RiskAssessmentExposedGroup
                        {
                            RiskAssessmentReportId = reportId,
                            Group = group,
                            CreationTime = created,
                            TenantId = tenantId,
                        });
                    }
                }

                foreach (var (column, measure) in ControlMeasures)
                {
                    if (Bit(reader, column))
                    {
                        measures.Add(new RiskAssessmentControlMeasure
                        {
                            RiskAssessmentReportId = reportId,
                            Measure = measure,
                            CreationTime = created,
                            TenantId = tenantId,
                        });
                    }
                }

                foreach (var (column, action) in ImprovementActions)
                {
                    if (Bit(reader, column))
                    {
                        actions.Add(new RiskAssessmentImprovementAction
                        {
                            RiskAssessmentReportId = reportId,
                            Recommendation = action,
                            CreationTime = created,
                            TenantId = tenantId,
                        });
                    }
                }

                foreach (var (column, group) in ProtectedGroups)
                {
                    if (Bit(reader, column))
                    {
                        protectedGroups.Add(new RiskAssessmentProtectedGroup
                        {
                            RiskAssessmentReportId = reportId,
                            Group = group,

                            // The legacy column is a flag, not a count. A number invented here
                            // would be a headcount nobody took.
                            Number = null,

                            CreationTime = created,
                            TenantId = tenantId,
                        });
                    }
                }

                foreach (var (column, type) in Participants)
                {
                    if (Text(reader, column) is not { } name)
                    {
                        continue;
                    }

                    participants.Add(new RiskAssessmentParticipant
                    {
                        RiskAssessmentReportId = reportId,
                        ParticipantType = type,

                        // Free text in the legacy row, so it stays text. Matching a name against
                        // the employee list would attribute a report to whoever shares the name.
                        CompanyEmployeeId = null,
                        FullName = Fit(context, "RiskAssessmentParticipant", "FullName", name) ?? string.Empty,
                        Title = null,

                        CreationTime = created,
                        TenantId = tenantId,
                    });
                }

                if (exposed.Count + measures.Count + actions.Count
                    + protectedGroups.Count + participants.Count >= BatchSize)
                {
                    await FlushAsync();
                    context.Logger.LogInformation("    risk report detail: {Written} written so far", written);
                }
            }
        }

        await FlushAsync();

        return new StepResult(read, written, 0, $"risk report detail: {written} written");
    }

    // ------------------------------------------------------------------ employee immunisations

    /// <summary>
    /// An immunisation belongs to the worker, not to the examination that recorded it.
    /// <para>
    /// The examination forms produced 2,836 <c>MedicalExamImmunization</c> rows, one per form per
    /// vaccination. A worker examined six times arrives with six identical tetanus records, and a
    /// physician asking "has this person had tetanus" wants one answer. The employee-level record
    /// is the distinct set, dated from the earliest form that reported it — the earliest, because
    /// that is when the worker said they had been vaccinated, not when somebody last wrote it down.
    /// </para>
    /// </summary>
    private static async Task<StepResult> EmployeeImmunisationsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var already = (await db.Set<EmployeeImmunization>()
                .Select(i => new { i.CompanyEmployeeId, i.ImmunizationType })
                .ToListAsync(cancellationToken))
            .Select(i => (i.CompanyEmployeeId, i.ImmunizationType))
            .ToHashSet();

        var rows = new List<EmployeeImmunization>();
        var read = 0;

        // Straight from the destination: the examination forms are already carried, and reading
        // them back is both simpler and truer than replaying the legacy columns a second time.
        await using (var connection = await context.OpenModernAsync(cancellationToken))
        await using (var command = new SqlCommand(
                         """
                         SELECT f.CompanyEmployeeId, i.ImmunizationType, MIN(f.ExaminationDate) AS FirstDate,
                                MIN(f.TenantId) AS TenantId
                         FROM ensa.MedicalExamImmunization AS i
                         JOIN ensa.MedicalExaminationForm AS f ON f.Id = i.MedicalExaminationFormId
                         GROUP BY f.CompanyEmployeeId, i.ImmunizationType
                         ORDER BY f.CompanyEmployeeId, i.ImmunizationType;
                         """, connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var employeeId = reader.GetInt32(0);
                var type = (ImmunizationType)reader.GetInt32(1);

                if (!already.Add((employeeId, type)))
                {
                    continue;
                }

                rows.Add(new EmployeeImmunization
                {
                    CompanyEmployeeId = employeeId,
                    ImmunizationType = type,
                    Date = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                    Description = null,
                    CreationTime = reader.IsDBNull(2) ? DateTime.Now : reader.GetDateTime(2),
                    TenantId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                });
            }
        }

        if (!context.DryRun && rows.Count > 0)
        {
            foreach (var chunk in rows.Chunk(500))
            {
                db.AddRange(chunk);
                await db.SaveChangesAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }
        }

        return new StepResult(
            read, rows.Count, read - rows.Count,
            $"employee immunisations: {rows.Count} written from {read} distinct (worker, vaccination) pair(s)");
    }

    // ------------------------------------------------------------------ helpers

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
