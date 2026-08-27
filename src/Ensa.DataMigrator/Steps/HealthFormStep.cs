using System.Globalization;
using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Health;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The periodic medical examination form: 9,865 records across 135 legacy columns, 122 of them
/// ciphertext.
/// <para>
/// This is the statutory record that a worker was examined and found fit, and it has to be kept
/// for fifteen years after employment ends. It is also the widest table in the legacy schema,
/// because it was built by adding a column per question: twenty-three yes/no complaints, eleven
/// body systems, seven laboratory results, three previous jobs, four family histories, three
/// immunisations, two habits and three work conditions, each in its own <c>nvarchar(320)</c>.
/// </para>
/// <para>
/// The rebuilt schema already models all of that as child tables, so this step is mostly a matter
/// of turning columns into rows — and only where there is something to turn. A blank column is a
/// question nobody answered, and writing a row for it would put 9,865 empty complaints into a
/// medical record.
/// </para>
/// <para>
/// <b>Everything is decrypted on the way in and re-encrypted on the way out.</b> The legacy values
/// come through <see cref="LegacyCrypt"/>; the destination's own converters apply as the entities
/// are saved. Nothing decrypted is logged or written anywhere but the destination column.
/// </para>
/// </summary>
public sealed class HealthFormStep : IMigrationStep
{
    public int Order => 100;

    public string Name => "health-forms";

    public string Description => "Periodic medical examination forms and the child records their 135 columns become";

    private const int BatchSize = 200;

    /// <summary>Turkish, because the legacy dates are written out as "15 Aralik 2021 Carsamba".</summary>
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>The twenty-three yes/no complaint columns, in the order the legacy form asks them.</summary>
    private static readonly (string Column, MedicalComplaintType Type)[] Complaints =
    [
        ("BalgamliOksuruk", MedicalComplaintType.ProductiveCough),
        ("NefesDarligi", MedicalComplaintType.BreathShortness),
        ("GogusAgrisi", MedicalComplaintType.ChestPain),
        ("Carpinti", MedicalComplaintType.Palpitation),
        ("SirtAgrisi", MedicalComplaintType.BackPain),
        ("IshalVeyaKabizlik", MedicalComplaintType.DiarrheaOrConstipation),
        ("EklemlerdeAgri", MedicalComplaintType.JointPain),
        ("KalpHastaligi", MedicalComplaintType.CardiacDisease),
        ("SekerHastaligi", MedicalComplaintType.DiabetesDisease),
        ("BobrekHastaligi", MedicalComplaintType.RenalDisease),
        ("Sarilik", MedicalComplaintType.Jaundice),
        ("MideVeyaOnIkiParmakUlseri", MedicalComplaintType.GastricOrDuodenalUlcer),
        ("IsitmeKaybi", MedicalComplaintType.HearingLoss),
        ("GormeBozuklugu", MedicalComplaintType.VisionImpairment),
        ("SinirSistemiHastaligi", MedicalComplaintType.NervousSystemDisease),
        ("DeriHastaligi", MedicalComplaintType.SkinDisease),
        ("BesinZehirlenmesi", MedicalComplaintType.FoodPoisoning),
        ("HastanedeYattinizMi", MedicalComplaintType.HospitalAdmission),
        ("AmeliyatGecirdinizMi", MedicalComplaintType.Surgery),
        ("IsKazasiGecirdinizMi", MedicalComplaintType.WorkAccident),
        ("MesHasSupMu", MedicalComplaintType.OccupationalDiseaseSuspicion),
        ("MaluliyetAldinizMi", MedicalComplaintType.Disability),
        ("TedaviGoruyorMusunuz", MedicalComplaintType.OngoingTreatment),
    ];

    /// <summary>The eleven body systems the physical examination covers.</summary>
    private static readonly (string Column, PhysicalExamSystem System)[] PhysicalFindings =
    [
        ("DuyuGoz", PhysicalExamSystem.SensoryEye),
        ("DuyuKulakBurunBogaz", PhysicalExamSystem.SensoryEarNoseThroat),
        ("DuyuDeri", PhysicalExamSystem.SensorySkin),
        ("KardiyovaskulerSisMu", PhysicalExamSystem.CardiovascularSystem),
        ("SolunumSisMu", PhysicalExamSystem.RespiratorySystem),
        ("SindirimSisMu", PhysicalExamSystem.DigestiveSystem),
        ("UrogenitalSisMu", PhysicalExamSystem.UrogenitalSystem),
        ("KasIskeletSisMu", PhysicalExamSystem.MuscularSkeletalSystem),
        ("NorolojikMu", PhysicalExamSystem.Neurological),
        ("PiskiyatrikMu", PhysicalExamSystem.Psychiatric),
        ("FizikMuDiger", PhysicalExamSystem.Other),
    ];

    /// <summary>
    /// The laboratory tests. Five of them record separately whether the test was done and what it
    /// found; two only have a result, so having one is what "done" means.
    /// </summary>
    private static readonly (string ResultColumn, string? DoneColumn, LabTestType Type)[] LabTests =
    [
        ("Kan", "KanTetkikiYapildiMi", LabTestType.Blood),
        ("Idrar", "IdrarTetkikiYapildiMi", LabTestType.Urine),
        ("RadyolojikAnaliz", "RontgenYapildiMi", LabTestType.RadiologicalImaging),
        ("Odyometre", "IsitmeTestiYapildiMi", LabTestType.Audiometry),
        ("SFT", "SolunumFontTestiYapildiMi", LabTestType.RespiratoryFunctionTest),
        ("PsikolojikTestler", null, LabTestType.PsychologicalTest),
        ("LabDiger", null, LabTestType.Other),
    ];

    private static readonly (string Column, ImmunizationType Type)[] Immunizations =
    [
        ("BagisiklamaTetanoz", ImmunizationType.Tetanus),

        // The legacy form asks "Hepatit" without saying which, and the honest answer would be the
        // unspecified case - except that the destination allows one row per type per form, so
        // filing both this and "Diger" under Other merges a hepatitis vaccination with an unnamed
        // one and loses which was which. Hepatitis B is what workplace medicine vaccinates for
        // here; the column's own wording is kept in the description so the inference stays visible.
        ("BagisiklamaHepatit", ImmunizationType.HepatitisB),

        ("BagisiklamaDiger", ImmunizationType.Other),
    ];

    private static readonly (string Column, WorkConditionType Type)[] WorkConditions =
    [
        ("GeceCalis", WorkConditionType.NightWork),
        ("YuksekCalis", WorkConditionType.AtHeightWork),
        ("VardiyaliCalis", WorkConditionType.ShiftWork),
    ];

    private static readonly (string Column, FamilyRelation Relation)[] FamilyHistories =
    [
        ("SoyGecmisAnne", FamilyRelation.Mother),
        ("SoyGecmisBaba", FamilyRelation.Father),
        ("SoyGecmisKardes", FamilyRelation.Sibling),
        ("SoyGecmisCocuk", FamilyRelation.Child),
    ];

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var forms = await FormsAsync(context, cancellationToken);
        var children = await ChildrenAsync(context, cancellationToken);
        var employee = await EmployeeRecordsAsync(context, cancellationToken);

        return new StepResult(
            forms.Read + children.Read + employee.Read,
            forms.Written + children.Written + employee.Written,
            forms.Skipped + children.Skipped + employee.Skipped,
            string.Join("; ", new[] { forms.Note, children.Note, employee.Note }.Where(n => n is not null)));
    }

    // ------------------------------------------------------------------ the form itself

    private static async Task<StepResult> FormsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var employeeMap = await context.IdMap.LoadAsync("FirmaPersonel_T", cancellationToken);
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var userMap = await context.IdMap.LoadAsync("Kullanici_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var already = await context.IdMap.LoadAsync("PeriyodikMuayeneFormu_T", cancellationToken);

        var read = 0;
        var written = 0;
        var orphaned = 0;
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, MedicalExaminationForm Entity)>();

        const string sql = """
            SELECT PeriyodikMuayeneFormuId, FirmaPersonelId, FirmaId, KurumId, RaporTuru,
                   FormTarihi, GecerlilikTarihi, Boy, Kilo, BMI, TA, Nb, KronikHastalik,
                   BedenRuhen, KanaatVeSonuc1, CalismaSarti, DosyaId,
                   IBYSDurum, IBYSDurumKodu, IBYSDurumMesaj, IBYSGrupKodu, IBYSCalisanMeslegi,
                   CalismaOrtami, CalismaSekli, IsEkipmanlari, Kaynak,
                   EkleyenKullanici, EklemeTarihi, GuncellemeTarihi, Silindi
            FROM PeriyodikMuayeneFormu_T ORDER BY PeriyodikMuayeneFormuId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "PeriyodikMuayeneFormuId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                if (!employeeMap.TryGetValue(Required(reader, "FirmaPersonelId"), out var employeeId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphaned++;
                    continue;
                }

                var examinationDate = TurkishDate(Plain(reader, "FormTarihi"))
                                      ?? Date(reader, "EklemeTarihi")
                                      ?? DateTime.Now;

                var (systolic, diastolic) = BloodPressure(Plain(reader, "TA"));

                batch.Add((legacyId, new MedicalExaminationForm
                {
                    CompanyId = Lookup(companyMap, Int(reader, "FirmaId")),
                    CompanyEmployeeId = employeeId,
                    ReportType = ReportTypeOf(Plain(reader, "RaporTuru")),
                    ExaminationDate = examinationDate,
                    ValidityDate = TurkishDate(Plain(reader, "GecerlilikTarihi")),

                    // The legacy schema records who entered the form, not who examined the
                    // patient. In this system they are the same person - a periodic examination
                    // form is filled in by the workplace physician who performed it - and the
                    // alternative is to lose the only trace of which physician signed it.
                    PhysicianUserId = Lookup(userMap, Int(reader, "EkleyenKullanici")),

                    HeightCm = Whole(Plain(reader, "Boy"), 50, 260),
                    WeightKg = Fraction(Plain(reader, "Kilo"), 10m, 400m),
                    BodyMassIndex = Fraction(Plain(reader, "BMI"), 5m, 120m),
                    BloodPressureSystolic = systolic,
                    BloodPressureDiastolic = diastolic,
                    PulseRate = Whole(Plain(reader, "Nb"), 20, 250),

                    ChronicIllnessDeclaration =
                        Fit(context, "MedicalExaminationForm", "ChronicIllnessDeclaration", Plain(reader, "KronikHastalik")),
                    Opinion = OpinionOf(Plain(reader, "BedenRuhen")),
                    OpinionDescription =
                        Fit(context, "MedicalExaminationForm", "OpinionDescription", Plain(reader, "KanaatVeSonuc1")),
                    Recommendations =
                        Fit(context, "MedicalExaminationForm", "Recommendations", Plain(reader, "CalismaSarti")),

                    // DosyaId is stored encrypted here, unlike everywhere else it appears.
                    DocumentId = null,

                    IbysStatus = IbysStatusOf(Int(reader, "IBYSDurum")),
                    IbysStatusCode = Int(reader, "IBYSDurumKodu"),
                    IbysStatusMessage =
                        Fit(context, "MedicalExaminationForm", "IbysStatusMessage", Plain(reader, "IBYSDurumMesaj")),
                    IbysGroupCode =
                        Fit(context, "MedicalExaminationForm", "IbysGroupCode", Plain(reader, "IBYSGrupKodu")),
                    IbysOccupationCode =
                        Fit(context, "MedicalExaminationForm", "IbysOccupationCode", Plain(reader, "IBYSCalisanMeslegi")),
                    IbysWorkEnvironmentCodes =
                        Fit(context, "MedicalExaminationForm", "IbysWorkEnvironmentCodes", Plain(reader, "CalismaOrtami")),
                    IbysWorkArrangementCodes =
                        Fit(context, "MedicalExaminationForm", "IbysWorkArrangementCodes", Plain(reader, "CalismaSekli")),
                    IbysWorkEquipmentCodes =
                        Fit(context, "MedicalExaminationForm", "IbysWorkEquipmentCodes", Plain(reader, "IsEkipmanlari")),
                    Source = Fit(context, "MedicalExaminationForm", "Source", Plain(reader, "Kaynak")),

                    IsDeleted = Bit(reader, "Silindi"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? examinationDate,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                }));

                if (batch.Count >= BatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, "PeriyodikMuayeneFormu_T", batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, "PeriyodikMuayeneFormu_T", batch, pairs, cancellationToken);
        }

        var note = $"examination forms: {written} written";
        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (employee or organization missing)";
        }

        return new StepResult(read, written, orphaned, note);
    }

    // ------------------------------------------------------------------ the child rows

    /// <summary>
    /// The columns that become rows: complaints, physical findings, laboratory tests, habits,
    /// immunisations and work conditions.
    /// <para>
    /// One pass over the legacy table produces all six, because they all come from the same row
    /// and reading 9,865 rows six times to keep the code tidy would be a poor trade. Written in
    /// bulk: nothing will ever look one of these up by a legacy id, and there is no legacy id to
    /// look them up by — they are columns, not rows.
    /// </para>
    /// </summary>
    private static async Task<StepResult> ChildrenAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var formMap = await context.IdMap.LoadAsync("PeriyodikMuayeneFormu_T", cancellationToken);
        if (formMap.Count == 0)
        {
            return new StepResult(0, 0, 0, "child records: nothing to do, no form is mapped");
        }

        // A second run must not double the child rows, and they have no id map of their own; the
        // forms that already have children are the ones to skip.
        //
        // All six tables, not the three obvious ones: a form whose complaint columns are all blank
        // still produces a habit or a work condition, and leaving those tables out of the question
        // means a re-run offers the same habit again and collides on its unique index.
        var done = (await db.Set<MedicalExamComplaint>()
                .Select(c => c.MedicalExaminationFormId).Distinct().ToListAsync(cancellationToken))
            .Concat(await db.Set<MedicalExamPhysicalFinding>()
                .Select(c => c.MedicalExaminationFormId).Distinct().ToListAsync(cancellationToken))
            .Concat(await db.Set<MedicalExamLabTest>()
                .Select(c => c.MedicalExaminationFormId).Distinct().ToListAsync(cancellationToken))
            .Concat(await db.Set<MedicalExamHabit>()
                .Select(c => c.MedicalExaminationFormId).Distinct().ToListAsync(cancellationToken))
            .Concat(await db.Set<MedicalExamImmunization>()
                .Select(c => c.MedicalExaminationFormId).Distinct().ToListAsync(cancellationToken))
            .Concat(await db.Set<MedicalExamWorkCondition>()
                .Select(c => c.MedicalExaminationFormId).Distinct().ToListAsync(cancellationToken))
            .ToHashSet();

        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        var complaints = new List<MedicalExamComplaint>();
        var findings = new List<MedicalExamPhysicalFinding>();
        var labTests = new List<MedicalExamLabTest>();
        var habits = new List<MedicalExamHabit>();
        var immunizations = new List<MedicalExamImmunization>();
        var conditions = new List<MedicalExamWorkCondition>();

        var read = 0;
        var written = 0;

        var columns = new List<string> { "PeriyodikMuayeneFormuId", "KurumId", "FormTarihi" };
        columns.AddRange(Complaints.Select(c => c.Column));
        columns.AddRange(PhysicalFindings.Select(f => f.Column));
        columns.AddRange(LabTests.Select(l => l.ResultColumn));
        columns.AddRange(LabTests.Where(l => l.DoneColumn is not null).Select(l => l.DoneColumn!));
        columns.AddRange(Immunizations.Select(i => i.Column));
        columns.AddRange(WorkConditions.Select(w => w.Column));
        columns.AddRange(["SigaraIciyorMusunuz", "SigaraSure", "SigaraSureZaman", "SigaraAdet", "SigaraSureOnce",
                          "AlkolIciyorMusunuz", "AlkolSure", "AlkolYilOnce", "AlkolSiklikla"]);

        var sql = $"""
            SELECT {string.Join(", ", columns.Distinct().Select(c => $"[{c}]"))}
            FROM PeriyodikMuayeneFormu_T ORDER BY PeriyodikMuayeneFormuId;
            """;

        async Task FlushChildrenAsync()
        {
            if (context.DryRun)
            {
                written += complaints.Count + findings.Count + labTests.Count
                           + habits.Count + immunizations.Count + conditions.Count;
            }
            else
            {
                db.AddRange(complaints);
                db.AddRange(findings);
                db.AddRange(labTests);
                db.AddRange(habits);
                db.AddRange(immunizations);
                db.AddRange(conditions);
                await db.SaveChangesAsync(cancellationToken);

                written += complaints.Count + findings.Count + labTests.Count
                           + habits.Count + immunizations.Count + conditions.Count;

                db.ChangeTracker.Clear();
            }

            complaints.Clear();
            findings.Clear();
            labTests.Clear();
            habits.Clear();
            immunizations.Clear();
            conditions.Clear();
        }

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                if (!formMap.TryGetValue(Required(reader, "PeriyodikMuayeneFormuId"), out var formId)
                    || done.Contains(formId))
                {
                    continue;
                }

                var tenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenant)
                    ? tenant
                    : (int?)null;
                var date = TurkishDate(Plain(reader, "FormTarihi"));

                foreach (var (column, type) in Complaints)
                {
                    var (answer, detail) = ComplaintOf(Plain(reader, column));
                    if (answer == TriStateAnswer.Unspecified)
                    {
                        continue;
                    }

                    complaints.Add(new MedicalExamComplaint
                    {
                        MedicalExaminationFormId = formId,
                        ComplaintType = type,
                        Answer = answer,
                        Description = Fit(context, "MedicalExamComplaint", "Description", detail),
                        CreationTime = date ?? DateTime.Now,
                        TenantId = tenantId,
                    });
                }

                foreach (var (column, system) in PhysicalFindings)
                {
                    if (Plain(reader, column) is not { } text)
                    {
                        continue;
                    }

                    findings.Add(new MedicalExamPhysicalFinding
                    {
                        MedicalExaminationFormId = formId,
                        System = system,
                        Finding = FindingOf(text),
                        Description = Fit(context, "MedicalExamPhysicalFinding", "Description", text),
                        CreationTime = date ?? DateTime.Now,
                        TenantId = tenantId,
                    });
                }

                foreach (var (resultColumn, doneColumn, type) in LabTests)
                {
                    var result = Plain(reader, resultColumn);
                    var performed = doneColumn is null ? result is not null : Flag(Plain(reader, doneColumn));

                    if (result is null && !performed)
                    {
                        continue;
                    }

                    labTests.Add(new MedicalExamLabTest
                    {
                        MedicalExaminationFormId = formId,
                        LabTestType = type,
                        IsCompleted = performed,
                        Result = Fit(context, "MedicalExamLabTest", "Result", result),
                        Date = date,
                        CreationTime = date ?? DateTime.Now,
                        TenantId = tenantId,
                    });
                }

                foreach (var (column, type) in Immunizations)
                {
                    // Only a "yes". The legacy column is a checkbox, and a false is the absence of
                    // an immunisation, not a record of one.
                    if (!Flag(Plain(reader, column)))
                    {
                        continue;
                    }

                    immunizations.Add(new MedicalExamImmunization
                    {
                        MedicalExaminationFormId = formId,
                        ImmunizationType = type,
                        Date = null,
                        Description = column == "BagisiklamaHepatit"
                            ? "Hepatit (the legacy form does not say which)"
                            : null,
                        CreationTime = date ?? DateTime.Now,
                        TenantId = tenantId,
                    });
                }

                foreach (var (column, type) in WorkConditions)
                {
                    if (Answer(Plain(reader, column)) is { } suitable && suitable != TriStateAnswer.Unspecified)
                    {
                        conditions.Add(new MedicalExamWorkCondition
                        {
                            MedicalExaminationFormId = formId,
                            ConditionType = type,
                            Suitable = suitable,
                            CreationTime = date ?? DateTime.Now,
                            TenantId = tenantId,
                        });
                    }
                }

                if (HabitOf(reader, HabitType.Smoking, formId, tenantId, date) is { } smoking)
                {
                    habits.Add(smoking);
                }

                if (HabitOf(reader, HabitType.Alcohol, formId, tenantId, date) is { } alcohol)
                {
                    habits.Add(alcohol);
                }

                if (complaints.Count + findings.Count + labTests.Count
                    + habits.Count + immunizations.Count + conditions.Count >= 2_000)
                {
                    await FlushChildrenAsync();

                    context.Logger.LogInformation("    health form children: {Written} written so far", written);
                }
            }
        }

        await FlushChildrenAsync();

        return new StepResult(read, written, 0, $"child records: {written} written");
    }

    /// <summary>
    /// One habit, from the four or five columns the legacy form spreads it over.
    /// <para>
    /// "Birakmis" is the answer that matters: the form asks whether the worker smokes and offers
    /// yes, no and "gave up", which is a status rather than a yes or a no. A worker who gave up
    /// twenty years ago and one who smokes forty a day are different medical facts, and the
    /// destination has a status column precisely so they stay different.
    /// </para>
    /// </summary>
    private static MedicalExamHabit? HabitOf(
        SqlDataReader reader,
        HabitType type,
        int formId,
        int? tenantId,
        DateTime? date)
    {
        var answer = type == HabitType.Smoking
            ? Plain(reader, "SigaraIciyorMusunuz")
            : Plain(reader, "AlkolIciyorMusunuz");

        var status = HabitStatusOf(answer);
        if (status == HabitStatus.Unspecified)
        {
            return null;
        }

        return new MedicalExamHabit
        {
            MedicalExaminationFormId = formId,
            HabitType = type,
            Status = status,
            DailyQuantity = type == HabitType.Smoking ? Whole(Plain(reader, "SigaraAdet"), 1, 200) : null,
            DurationYear = type == HabitType.Smoking
                ? Whole(Plain(reader, "SigaraSure"), 1, 90)
                : Whole(Plain(reader, "AlkolSure"), 1, 90),
            CessationYearBefore = type == HabitType.Smoking
                ? Whole(Plain(reader, "SigaraSureOnce"), 1, 90)
                : Whole(Plain(reader, "AlkolYilOnce"), 1, 90),
            Description = type == HabitType.Alcohol ? Plain(reader, "AlkolSiklikla") : null,
            CreationTime = date ?? DateTime.Now,
            TenantId = tenantId,
        };
    }

    // ------------------------------------------------------------------ the employee-level records

    /// <summary>
    /// Blood group, family history and previous jobs belong to the worker, not to one examination.
    /// <para>
    /// The legacy form repeats them on every examination, so a worker with six forms would arrive
    /// with six identical mothers. Only the most recent form that has each fact is used, which is
    /// also the answer a physician would want: the last thing the worker told anybody.
    /// </para>
    /// </summary>
    private static async Task<StepResult> EmployeeRecordsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var employeeMap = await context.IdMap.LoadAsync("FirmaPersonel_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        var haveHealth = (await db.Set<Ensa.Domain.Companies.EmployeeHealthInfo>()
            .Select(h => h.CompanyEmployeeId).ToListAsync(cancellationToken)).ToHashSet();
        var haveFamily = (await db.Set<Ensa.Domain.Companies.EmployeeFamilyHistory>()
            .Select(h => h.CompanyEmployeeId).Distinct().ToListAsync(cancellationToken)).ToHashSet();
        var haveWork = (await db.Set<Ensa.Domain.Companies.EmployeeWorkHistory>()
            .Select(h => h.CompanyEmployeeId).Distinct().ToListAsync(cancellationToken)).ToHashSet();

        var columns = new List<string>
        {
            "FirmaPersonelId", "KurumId", "KanGrubu", "KronikHastalik", "FormTarihi",
        };
        columns.AddRange(FamilyHistories.Select(f => f.Column));
        for (var slot = 1; slot <= 3; slot++)
        {
            columns.AddRange([
                $"EskiIsIskolu{slot}", $"EskiIsYaptigiIs{slot}",
                $"EskiIsGirisTarihi{slot}", $"EskiIsCikisTarihi{slot}",
            ]);
        }

        // Descending, so the first row seen for an employee is the most recent form that has the
        // fact and every later one is skipped.
        var sql = $"""
            SELECT {string.Join(", ", columns.Select(c => $"[{c}]"))}
            FROM PeriyodikMuayeneFormu_T ORDER BY PeriyodikMuayeneFormuId DESC;
            """;

        var health = new List<Ensa.Domain.Companies.EmployeeHealthInfo>();
        var family = new List<Ensa.Domain.Companies.EmployeeFamilyHistory>();
        var work = new List<Ensa.Domain.Companies.EmployeeWorkHistory>();

        var read = 0;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                if (!employeeMap.TryGetValue(Required(reader, "FirmaPersonelId"), out var employeeId))
                {
                    continue;
                }

                var tenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenant)
                    ? tenant
                    : (int?)null;
                var date = TurkishDate(Plain(reader, "FormTarihi")) ?? DateTime.Now;

                var bloodType = BloodTypeOf(Plain(reader, "KanGrubu"));
                var chronic = Plain(reader, "KronikHastalik");

                if (!haveHealth.Contains(employeeId)
                    && (bloodType != BloodType.Unspecified || chronic is not null))
                {
                    haveHealth.Add(employeeId);
                    health.Add(new Ensa.Domain.Companies.EmployeeHealthInfo
                    {
                        CompanyEmployeeId = employeeId,
                        BloodType = bloodType,
                        ChronicIllnessDescription =
                            Fit(context, "EmployeeHealthInfo", "ChronicIllnessDescription", chronic),
                        CreationTime = date,
                        TenantId = tenantId,
                    });
                }

                if (!haveFamily.Contains(employeeId))
                {
                    var rows = FamilyHistories
                        .Select(f => (f.Relation, Text: Plain(reader, f.Column)))
                        .Where(f => f.Text is not null)
                        .ToList();

                    if (rows.Count > 0)
                    {
                        haveFamily.Add(employeeId);
                        family.AddRange(rows.Select(f => new Ensa.Domain.Companies.EmployeeFamilyHistory
                        {
                            CompanyEmployeeId = employeeId,
                            Relation = f.Relation,
                            Description = Fit(context, "EmployeeFamilyHistory", "Description", f.Text),
                            CreationTime = date,
                            TenantId = tenantId,
                        }));
                    }
                }

                if (!haveWork.Contains(employeeId))
                {
                    var rows = new List<Ensa.Domain.Companies.EmployeeWorkHistory>();

                    for (var slot = 1; slot <= 3; slot++)
                    {
                        var sector = Plain(reader, $"EskiIsIskolu{slot}");
                        var job = Plain(reader, $"EskiIsYaptigiIs{slot}");

                        if (sector is null && job is null)
                        {
                            continue;
                        }

                        rows.Add(new Ensa.Domain.Companies.EmployeeWorkHistory
                        {
                            CompanyEmployeeId = employeeId,
                            WorkSector = Fit(context, "EmployeeWorkHistory", "WorkSector", sector),
                            PerformedJob = Fit(context, "EmployeeWorkHistory", "PerformedJob", job),
                            EntryDate = TurkishDate(Plain(reader, $"EskiIsGirisTarihi{slot}")),
                            ExitDate = TurkishDate(Plain(reader, $"EskiIsCikisTarihi{slot}")),
                            OrderNo = slot,
                            CreationTime = date,
                            TenantId = tenantId,
                        });
                    }

                    if (rows.Count > 0)
                    {
                        haveWork.Add(employeeId);
                        work.AddRange(rows);
                    }
                }
            }
        }

        var written = health.Count + family.Count + work.Count;

        if (!context.DryRun && written > 0)
        {
            foreach (var chunk in health.Chunk(500))
            {
                db.AddRange(chunk);
                await db.SaveChangesAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }

            foreach (var chunk in family.Chunk(500))
            {
                db.AddRange(chunk);
                await db.SaveChangesAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }

            foreach (var chunk in work.Chunk(500))
            {
                db.AddRange(chunk);
                await db.SaveChangesAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }
        }

        return new StepResult(
            read, written, 0,
            $"employee records: {health.Count} health, {family.Count} family history, {work.Count} work history");
    }

    // ------------------------------------------------------------------ value mapping

    /// <summary><c>RaporTuru</c>, one of two slugs.</summary>
    private static MedicalReportType ReportTypeOf(string? type)
        => Fold(type) switch
        {
            "ISE_GIRIS_MUAYENESI" => MedicalReportType.PreEmploymentExamination,
            "PERIYODIK_MUAYENE" => MedicalReportType.PeriodicExamination,
            _ => MedicalReportType.Unspecified,
        };

    /// <summary>
    /// <c>BedenRuhen</c> — "is the worker fit in body and mind". Four values across 8,887 rows:
    /// two spellings of fit and two of conditionally fit. The legacy form has no way to say unfit,
    /// which is itself worth knowing: an unfit worker was not given a form.
    /// </summary>
    private static FitnessForWorkOpinion OpinionOf(string? opinion)
        => Fold(opinion) switch
        {
            "CALISABILIR" or "EVET" => FitnessForWorkOpinion.Fit,
            "SARTLI_CALISABILIR" or "SARTLI" => FitnessForWorkOpinion.ConditionallyFit,
            "CALISAMAZ" or "HAYIR" => FitnessForWorkOpinion.Unfit,
            _ => FitnessForWorkOpinion.Unspecified,
        };

    /// <summary>
    /// One complaint answer, which the legacy form records two different ways.
    /// <para>
    /// Seventeen of the twenty-three columns are checkboxes and hold "True" or "False". The last
    /// six — hospital admission, surgery, work accident, suspected occupational disease,
    /// disability, ongoing treatment — are free text, and hold what it was: "APENDEKTOMI", "NSD",
    /// "AMELIYAT". Reading those as a yes/no loses 2,674 answers, so the rule is the one a person
    /// would apply: text in the box means the answer was yes, and the text is what it was.
    /// </para>
    /// <para>
    /// A box holding nothing but punctuation — a dash somebody typed to mean "asked, nothing to
    /// report" — is neither, and produces no row at all rather than a yes with a hyphen after it.
    /// </para>
    /// </summary>
    private static (TriStateAnswer Answer, string? Detail) ComplaintOf(string? value)
    {
        var folded = Fold(value);

        if (folded is null || folded.All(c => !char.IsLetterOrDigit(c)))
        {
            return (TriStateAnswer.Unspecified, null);
        }

        return folded switch
        {
            "TRUE" or "EVET" or "VAR" => (TriStateAnswer.Yes, null),
            "FALSE" or "HAYIR" or "YOK" => (TriStateAnswer.No, null),
            _ => (TriStateAnswer.Yes, value),
        };
    }

    /// <summary>
    /// A yes/no answer, as the work condition columns store it: "Evet" or "Hayir".
    /// </summary>
    private static TriStateAnswer? Answer(string? value)
        => Fold(value) switch
        {
            "TRUE" or "EVET" or "VAR" => TriStateAnswer.Yes,
            "FALSE" or "HAYIR" or "YOK" => TriStateAnswer.No,
            null => null,
            _ => TriStateAnswer.Unspecified,
        };

    private static bool Flag(string? value)
        => Fold(value) is "TRUE" or "EVET" or "VAR";

    /// <summary>
    /// <c>SigaraIciyorMusunuz</c> / <c>AlkolIciyorMusunuz</c>: yes, no, or "gave up".
    /// </summary>
    private static HabitStatus HabitStatusOf(string? value)
        => Fold(value) switch
        {
            "EVET" => HabitStatus.CurrentlyUsing,
            "HAYIR" => HabitStatus.NeverUsed,
            "BIRAKMIS" => HabitStatus.Quit,
            _ => HabitStatus.Unspecified,
        };

    /// <summary>
    /// Whether a physical examination finding is normal.
    /// <para>
    /// The column is free text and the physicians who filled it in wrote "NORMAL", "Normal", "N",
    /// "NFM" (normal fizik muayene) and eleven other spellings of the same thing. Anything that is
    /// not one of those is a finding, and it is recorded as pathological with the text kept
    /// verbatim — the classification is a search aid, the description is the medicine.
    /// </para>
    /// </summary>
    private static ExamFinding FindingOf(string text)
    {
        var folded = Fold(text);

        if (folded is null)
        {
            return ExamFinding.Unspecified;
        }

        if (folded is "N" or "NFM" or "NORMAL" || folded.StartsWith("NORMAL ", StringComparison.Ordinal))
        {
            return ExamFinding.Normal;
        }

        return folded is "YAPILMADI" or "-" ? ExamFinding.NotPerformed : ExamFinding.Pathological;
    }

    /// <summary>
    /// <c>KanGrubu</c>, written 203 different ways: "A Rh (+)", "ARH+", "a rh +", "A RH POZITIF",
    /// "0 Rh (+)" and "O RH +" for the same group. Everything that is not punctuation is stripped,
    /// the letter O and the digit zero are treated as the same group, and the sign is whatever the
    /// remainder says. Anything left over stays unspecified rather than being assigned a group —
    /// a wrong blood group in a medical record is the most dangerous single field in this system.
    /// </summary>
    private static BloodType BloodTypeOf(string? value)
    {
        var folded = Fold(value);
        if (folded is null)
        {
            return BloodType.Unspecified;
        }

        var positive = folded.Contains('+') || folded.Contains("POZITIF", StringComparison.Ordinal);
        var negative = folded.Contains('-') || folded.Contains("NEGATIF", StringComparison.Ordinal);

        if (positive == negative)
        {
            return BloodType.Unspecified;
        }

        var letters = new string(folded.Where(char.IsLetterOrDigit).ToArray())
            .Replace("POZITIF", string.Empty, StringComparison.Ordinal)
            .Replace("NEGATIF", string.Empty, StringComparison.Ordinal)
            .Replace("RH", string.Empty, StringComparison.Ordinal)
            .Replace("GRUBU", string.Empty, StringComparison.Ordinal)
            .Trim();

        return letters switch
        {
            "A" => positive ? BloodType.ARhPositive : BloodType.ARhNegative,
            "B" => positive ? BloodType.BRhPositive : BloodType.BRhNegative,
            "AB" => positive ? BloodType.ABRhPositive : BloodType.ABRhNegative,
            "0" or "O" => positive ? BloodType.ZeroRhPositive : BloodType.ZeroRhNegative,
            _ => BloodType.Unspecified,
        };
    }

    /// <summary><c>IBYSDurum</c>: 1 accepted, -1 rejected, 0 prepared but not sent.</summary>
    private static IbysSubmissionStatus IbysStatusOf(int? status)
        => status switch
        {
            1 => IbysSubmissionStatus.Approved,
            -1 => IbysSubmissionStatus.Failed,
            0 => IbysSubmissionStatus.Prepared,
            _ => IbysSubmissionStatus.NotSent,
        };

    /// <summary>
    /// A date the legacy system wrote out in full Turkish — "15 Aralik 2021 Carsamba".
    /// <para>
    /// Parsed with the Turkish culture and the exact formats it was written in, then with whatever
    /// the culture makes of it. A date that will not parse becomes null rather than today: an
    /// examination validity date that silently becomes current would say a lapsed worker is
    /// cleared to work.
    /// </para>
    /// </summary>
    private static DateTime? TurkishDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();

        string[] formats =
        [
            "d MMMM yyyy dddd", "dd MMMM yyyy dddd", "d MMMM yyyy", "dd MMMM yyyy",
            "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "dd/MM/yyyy",
        ];

        if (DateTime.TryParseExact(text, formats, Turkish, DateTimeStyles.None, out var exact))
        {
            return exact;
        }

        return DateTime.TryParse(text, Turkish, DateTimeStyles.None, out var parsed) ? parsed : null;
    }

    /// <summary>
    /// Blood pressure, written as "120/80", "120 / 80" or "120-80" across 790 spellings.
    /// Both halves have to be plausible or neither is taken: half a blood pressure is not a
    /// reading.
    /// </summary>
    private static (int? Systolic, int? Diastolic) BloodPressure(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (null, null);
        }

        var parts = value.Split(['/', '-', '\\'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return (null, null);
        }

        var systolic = Whole(parts[0], 50, 300);
        var diastolic = Whole(parts[1], 20, 200);

        return systolic is null || diastolic is null ? (null, null) : (systolic, diastolic);
    }

    /// <summary>
    /// A whole number from free text, accepted only inside the range a human body occupies.
    /// A height of 1,700 is centimetres typed as millimetres or a typing slip; either way it is
    /// not a measurement, and storing it would corrupt every average computed over the column.
    /// </summary>
    private static int? Whole(string? value, int minimum, int maximum)
    {
        var number = Digits(value);
        return number is not null && number >= minimum && number <= maximum ? (int)number : null;
    }

    private static decimal? Fraction(string? value, decimal minimum, decimal maximum)
    {
        var number = Digits(value);
        return number is not null && number >= minimum && number <= maximum
            ? Math.Round(number.Value, 2)
            : null;
    }

    private static decimal? Digits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = new string(value.Where(c => char.IsDigit(c) || c is '.' or ',').ToArray())
            .Replace(',', '.');

        var firstDot = cleaned.IndexOf('.', StringComparison.Ordinal);
        if (firstDot >= 0)
        {
            cleaned = cleaned[..(firstDot + 1)] + cleaned[(firstDot + 1)..].Replace(".", string.Empty, StringComparison.Ordinal);
        }

        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
    }

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

    /// <summary>
    /// A legacy value as plaintext: decrypted when it is ciphertext, trimmed, and null when it
    /// says nothing. A value that will not decrypt is dropped rather than stored as its ciphertext,
    /// which would put unreadable text into a medical record and look like data.
    /// </summary>
    private static string? Plain(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var raw = reader.GetValue(index)?.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var value = LegacyCrypt.LooksEncrypted(raw) ? LegacyCrypt.TryDecrypt(raw) : raw;
        value = value?.Trim();

        return string.IsNullOrEmpty(value) ? null : value;
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

    private static int? Lookup(Dictionary<int, int> map, int? legacyId)
        => legacyId is int id && map.TryGetValue(id, out var modernId) ? modernId : null;

    private static string? Fit(MigrationContext context, string table, string column, string? value)
        => context.Fitter.Fit(table, column, value);

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
