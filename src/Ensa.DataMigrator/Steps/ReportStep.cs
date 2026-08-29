using System.Globalization;
using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Companies;
using Ensa.Domain.Reports;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The four reports an OSGB produces about itself and its customers: the activity report, the
/// year-end review, the monthly company check and the ministry's specialist-hours report.
/// </summary>
/// <remarks>
/// These are the outputs of the system rather than its inputs — the documents an inspector asks
/// for. They are also where the legacy schema is at its loosest: a line type is a Turkish string,
/// a date is text in whatever format somebody typed, and a report's sub-lines are a JSON blob in
/// a column. Each of those is handled explicitly below rather than being allowed to fail quietly.
/// </remarks>
public sealed class ReportStep : IMigrationStep
{
    public int Order => 106;

    public string Name => "reports";

    public string Description => "Activity reports, year-end reviews, company checks and the specialist-hours report";

    private const int BatchSize = 500;

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var results = new List<StepResult>
        {
            await ControlItemsAsync(context, cancellationToken),
            await CompanyChecksAsync(context, cancellationToken),
            await CompanyCheckLinesAsync(context, cancellationToken),
            await ActivityReportsAsync(context, cancellationToken),
            await ActivityReportLinesAsync(context, cancellationToken),
            await YearEndReviewsAsync(context, cancellationToken),
            await YearEndReviewLinesAsync(context, cancellationToken),
            await OhsReportsAsync(context, cancellationToken),
            await OhsReportBreakdownsAsync(context, cancellationToken),
        };

        return new StepResult(
            results.Sum(r => r.Read),
            results.Sum(r => r.Written),
            results.Sum(r => r.Skipped),
            string.Join("; ", results.Select(r => r.Note).Where(note => note is not null)));
    }

    // ------------------------------------------------------------------ company checks

    private static async Task<StepResult> ControlItemsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<ControlItem>(
            context, "Kontrol_T", "control items",
            "SELECT KontrolId, KontrolAdi, Periyot, Sira, Aktif, KurumId, EklemeTarihi, GuncellemeTarihi FROM Kontrol_T ORDER BY KontrolId;",
            "KontrolId",
            (reader, _) =>
            {
                var (unit, value) = PeriodOf(Text(reader, "Periyot"));

                return new ControlItem
                {
                    ControlItemName = Fit(context, "ControlItem", "ControlItemName", Text(reader, "KontrolAdi")) ?? string.Empty,
                    PeriodId = null,
                    PeriodUnit = unit,
                    PeriodValue = value,
                    SortOrder = Int(reader, "Sira") ?? 0,
                    IsActive = Bit(reader, "Aktif"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = Int(reader, "KurumId") is int legacyTenantId
                               && organizationMap.TryGetValue(legacyTenantId, out var tenantId)
                        ? tenantId
                        : null,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> CompanyChecksAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var documentMap = await context.IdMap.LoadAsync("Dosya_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<CompanyCheck>(
            context, "FirmaKontrol_T", "company checks",
            """
            SELECT FirmaKontrolId, FirmaId, KontrolAyi, KontrolTarihi, Durum, DosyaId, Silindi,
                   KurumId, EklemeTarihi, GuncellemeTarihi
            FROM FirmaKontrol_T ORDER BY FirmaKontrolId;
            """,
            "FirmaKontrolId",
            (reader, orphan) =>
            {
                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new CompanyCheck
                {
                    CompanyId = companyId,
                    CheckMonth = Date(reader, "KontrolAyi") ?? DateTime.Now,
                    ControlItemDate = Date(reader, "KontrolTarihi"),
                    Status = CheckStatusOf(Text(reader, "Durum")),
                    DocumentId = Lookup(documentMap, Int(reader, "DosyaId")),
                    IsDeleted = Bit(reader, "Silindi"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> CompanyCheckLinesAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var checkMap = await context.IdMap.LoadAsync("FirmaKontrol_T", cancellationToken);
        var itemMap = await context.IdMap.LoadAsync("Kontrol_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<CompanyCheckLine>(
            context, "FirmaKontrolSatir_T", "company check lines",
            """
            SELECT KontrolSatirId, FirmaKontrolId, KontrolId, KontrolDurum, Durum, KurumId,
                   EklemeTarihi, GuncellemeTarihi
            FROM FirmaKontrolSatir_T ORDER BY KontrolSatirId;
            """,
            "KontrolSatirId",
            (reader, orphan) =>
            {
                if (!checkMap.TryGetValue(Required(reader, "FirmaKontrolId"), out var checkId)
                    || !itemMap.TryGetValue(Required(reader, "KontrolId"), out var itemId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new CompanyCheckLine
                {
                    CompanyControlItemId = checkId,
                    ControlItemId = itemId,
                    ControlItemStatus = Bit(reader, "KontrolDurum"),
                    Status = CheckStatusOf(Text(reader, "Durum")),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ activity reports

    private static async Task<StepResult> ActivityReportsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<ActivityReport>(
            context, "FaliyetRaporu_T", "activity reports",
            """
            SELECT RaporId, FirmaId, RaporTuru, RaporAdi, RaporBaslangic, RaporBitis,
                   IsDeleted, KurumId, EklemeTarihi
            FROM FaliyetRaporu_T ORDER BY RaporId;
            """,
            "RaporId",
            (reader, orphan) =>
            {
                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new ActivityReport
                {
                    CompanyId = companyId,

                    // "Firma Rapor" and "Sube Rapor" say whose report it is, not what period it
                    // covers, which is what ActivityReportType asks. The period is in the two date
                    // columns; nothing in the legacy row answers the question, so it is not
                    // answered.
                    ReportType = ActivityReportType.Unspecified,

                    ReportName = Fit(context, "ActivityReport", "ReportName", Text(reader, "RaporAdi")) ?? string.Empty,
                    ReportStart = Date(reader, "RaporBaslangic") ?? DateTime.Now,
                    ReportEnd = Date(reader, "RaporBitis") ?? DateTime.Now,
                    IsDeleted = Bit(reader, "IsDeleted"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> ActivityReportLinesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var reportMap = await context.IdMap.LoadAsync("FaliyetRaporu_T", cancellationToken);
        var reportTenants = await LoadTenantsAsync(context, "ensa.ActivityReport", cancellationToken);

        var ordinals = new Dictionary<int, int>();
        var unknownType = 0;

        var result = await CopyAsync<ActivityReportLine>(
            context, "FaaliyetRaporSatir_T", "activity report lines",
            "SELECT SatirId, RaporId, SatirTuru, Metin, Deger1, Deger2, Deger3 FROM FaaliyetRaporSatir_T ORDER BY RaporId, SatirId;",
            "SatirId",
            (reader, orphan) =>
            {
                var legacyReportId = Required(reader, "RaporId");

                if (!reportMap.TryGetValue(legacyReportId, out var reportId))
                {
                    orphan();
                    return null;
                }

                var lineType = LineTypeOf(Text(reader, "SatirTuru"));
                if (lineType is null)
                {
                    unknownType++;
                    orphan();
                    return null;
                }

                ordinals.TryGetValue(legacyReportId, out var ordinal);
                ordinals[legacyReportId] = ordinal + 1;

                return new ActivityReportLine
                {
                    ActivityReportId = reportId,
                    LineType = lineType.Value,
                    Text = Fit(context, "ActivityReportLine", "Text", Text(reader, "Metin")),
                    Value1 = Fit(context, "ActivityReportLine", "Value1", Text(reader, "Deger1")),
                    Value2 = Fit(context, "ActivityReportLine", "Value2", Text(reader, "Deger2")),
                    Value3 = Fit(context, "ActivityReportLine", "Value3", Text(reader, "Deger3")),
                    OrderNo = ordinal + 1,
                    CreationTime = DateTime.Now,
                    TenantId = reportTenants.GetValueOrDefault(reportId),
                };
            },
            cancellationToken);

        var note = result.Note;
        if (unknownType > 0)
        {
            note += $" ({unknownType} of them a line type the destination has no member for)";
        }

        return result with { Note = note };
    }

    // ------------------------------------------------------------------ year-end review

    private static async Task<StepResult> YearEndReviewsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<YearEndReviewReport>(
            context, "YSDRaporu_T", "year-end reviews",
            """
            SELECT RaporId, RaporBasligi, ErkekCalisan, KadinCalisan, GencCalisan, CocukCalisan,
                   Tarih, Uzman, Hekim, Vekil, Aktif, IsDeleted, FirmaId, KurumId,
                   EklemeTarihi, GuncellemeTarihi
            FROM YSDRaporu_T ORDER BY RaporId;
            """,
            "RaporId",
            (reader, orphan) =>
            {
                if (Int(reader, "FirmaId") is not int legacyCompanyId
                    || !companyMap.TryGetValue(legacyCompanyId, out var companyId))
                {
                    orphan();
                    return null;
                }

                return new YearEndReviewReport
                {
                    ReportTitle = Fit(context, "YearEndReviewReport", "ReportTitle", Text(reader, "RaporBasligi")) ?? string.Empty,
                    CompanyId = companyId,
                    MaleWorker = Int(reader, "ErkekCalisan"),
                    FemaleWorker = Int(reader, "KadinCalisan"),
                    YoungWorker = Int(reader, "GencCalisan"),
                    ChildWorker = Int(reader, "CocukCalisan"),
                    ReportDate = DashDate(Text(reader, "Tarih")) ?? Date(reader, "EklemeTarihi") ?? DateTime.Now,

                    // The three names are free text in the legacy row, not references. The user
                    // ids stay unset rather than being matched by name: two specialists share a
                    // name often enough that a match would attribute somebody else's report.
                    SpecialistUserId = null,
                    SpecialistFullName = Fit(context, "YearEndReviewReport", "SpecialistFullName", Text(reader, "Uzman")),
                    PhysicianUserId = null,
                    PhysicianFullName = Fit(context, "YearEndReviewReport", "PhysicianFullName", Text(reader, "Hekim")),
                    DeputyFullName = Fit(context, "YearEndReviewReport", "DeputyFullName", Text(reader, "Vekil")),

                    IsActive = Bit(reader, "Aktif"),
                    IsDeleted = Bit(reader, "IsDeleted"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = Int(reader, "KurumId") is int legacyTenantId
                               && organizationMap.TryGetValue(legacyTenantId, out var tenantId)
                        ? tenantId
                        : null,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> YearEndReviewLinesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var reportMap = await context.IdMap.LoadAsync("YSDRaporu_T", cancellationToken);
        var reportTenants = await LoadTenantsAsync(context, "ensa.YearEndReviewReport", cancellationToken);

        return await CopyAsync<YearEndReviewLine>(
            context, "YSDRSatirlari_T", "year-end review lines",
            """
            SELECT RaporSatirId, RaporId, SiraNo, Calisma, Tarih, KisiveUnvan, TekrarSayisi,
                   KullanilanYontem, SonucveYorum, Aktif, IsDeleted, EklemeTarihi, GuncellemeTarihi
            FROM YSDRSatirlari_T ORDER BY RaporSatirId;
            """,
            "RaporSatirId",
            (reader, orphan) =>
            {
                if (Int(reader, "RaporId") is not int legacyReportId
                    || !reportMap.TryGetValue(legacyReportId, out var reportId))
                {
                    orphan();
                    return null;
                }

                return new YearEndReviewLine
                {
                    YearEndReviewReportId = reportId,
                    OrderNo = Int(reader, "SiraNo") ?? 0,
                    Date = DashDate(Text(reader, "Tarih")),
                    DateText = Fit(context, "YearEndReviewLine", "DateText", Text(reader, "Tarih")),
                    Work = Fit(context, "YearEndReviewLine", "Work", Text(reader, "Calisma")),
                    PersonAndTitle = Fit(context, "YearEndReviewLine", "PersonAndTitle", Text(reader, "KisiveUnvan")),
                    RepeatCount = Fit(context, "YearEndReviewLine", "RepeatCount", Text(reader, "TekrarSayisi")),
                    UsedMethod = Fit(context, "YearEndReviewLine", "UsedMethod", Text(reader, "KullanilanYontem")),
                    ResultAndComment = Fit(context, "YearEndReviewLine", "ResultAndComment", Text(reader, "SonucveYorum")),

                    // AltCalismalarJson holds a report's sub-lines as JSON, and ParentLineId is
                    // where they belong. Unpacking it needs the parent's modern id, which does not
                    // exist while the parents are being written, so it is a job of its own rather
                    // than a guess made here.
                    ParentLineId = null,

                    IsActive = Bit(reader, "Aktif"),
                    IsDeleted = Bit(reader, "IsDeleted"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = reportTenants.GetValueOrDefault(reportId),
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ the specialist-hours report

    /// <summary>
    /// <c>ISGRapor_T</c> to <see cref="OhsReport"/> — how many minutes each specialist and
    /// physician owed and used, which is what the ministry audits an OSGB on.
    /// </summary>
    private static async Task<StepResult> OhsReportsAsync(MigrationContext context, CancellationToken cancellationToken)
    {
        var officeMap = await context.IdMap.LoadAsync("Ofisler_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<OhsReport>(
            context, "ISGRapor_T", "specialist-hours reports",
            """
            SELECT ISGRaporId, OfisId, ModulArsivDetayId, TcKimlikNo, PersonelAdi, PersonelTuru,
                   GorevTuru, ToplamAylikFazlaMesaiSuresi, ToplamDakika, KullanilanAylikDakika, KurumId
            FROM ISGRapor_T ORDER BY ISGRaporId;
            """,
            "ISGRaporId",
            (reader, orphan) =>
            {
                if (!officeMap.TryGetValue(Required(reader, "OfisId"), out var officeId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new OhsReport
                {
                    OfficeId = officeId,

                    // The archive detail is a legacy id into a table this migration has not
                    // reached. Carried as it stands rather than translated, because it is the
                    // only trace of which archived report run produced the row.
                    ModuleArchiveDetailId = Required(reader, "ModulArsivDetayId"),

                    NationalId = Fit(context, "OhsReport", "NationalId", Text(reader, "TcKimlikNo")) ?? string.Empty,
                    EmployeeName = Fit(context, "OhsReport", "EmployeeName", Text(reader, "PersonelAdi")) ?? string.Empty,
                    StaffRole = StaffRoleOf(Text(reader, "PersonelTuru")),
                    DutyType = AssignmentTypeOf(Text(reader, "GorevTuru")),
                    TotalMonthlyOvertimeDuration = Int(reader, "ToplamAylikFazlaMesaiSuresi") ?? 0,
                    TotalMinutes = Int(reader, "ToplamDakika") ?? 0,
                    UsedMonthlyMinutes = Int(reader, "KullanilanAylikDakika") ?? 0,
                    CreationTime = DateTime.Now,
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    /// <summary>
    /// The three hazard-class counts each specialist-hours row carries, as rows of their own.
    /// </summary>
    private static async Task<StepResult> OhsReportBreakdownsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var reportMap = await context.IdMap.LoadAsync("ISGRapor_T", cancellationToken);
        if (reportMap.Count == 0)
        {
            return new StepResult(0, 0, 0, null);
        }

        var done = (await db.Set<OhsReportHazardClassBreakdown>()
            .Select(b => b.OhsReportId).Distinct().ToListAsync(cancellationToken)).ToHashSet();

        var reportTenants = await LoadTenantsAsync(context, "ensa.OhsReport", cancellationToken);

        (string Column, HazardClass Class)[] classes =
        [
            ("AzTehlikeliAdet", HazardClass.LowHazard),
            ("TehlikeliAdet", HazardClass.Hazardous),
            ("CokTehlikeliAdet", HazardClass.VeryHazardous),
        ];

        var read = 0;
        var written = 0;
        var batch = new List<OhsReportHazardClassBreakdown>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
                         "SELECT ISGRaporId, AzTehlikeliAdet, TehlikeliAdet, CokTehlikeliAdet FROM ISGRapor_T ORDER BY ISGRaporId;",
                         connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                if (!reportMap.TryGetValue(Required(reader, "ISGRaporId"), out var reportId) || done.Contains(reportId))
                {
                    continue;
                }

                foreach (var (column, hazardClass) in classes)
                {
                    batch.Add(new OhsReportHazardClassBreakdown
                    {
                        OhsReportId = reportId,
                        HazardClass = hazardClass,
                        CompanyCount = Int(reader, column) ?? 0,
                        CreationTime = DateTime.Now,
                        TenantId = reportTenants.GetValueOrDefault(reportId),
                    });
                }

                if (batch.Count >= 2_000 && !context.DryRun)
                {
                    db.AddRange(batch);
                    await db.SaveChangesAsync(cancellationToken);
                    written += batch.Count;
                    batch.Clear();
                    db.ChangeTracker.Clear();
                }
            }
        }

        if (batch.Count > 0)
        {
            if (!context.DryRun)
            {
                db.AddRange(batch);
                await db.SaveChangesAsync(cancellationToken);
            }

            written += batch.Count;
        }

        return new StepResult(read, written, 0, $"hazard class breakdowns: {written} written");
    }

    // ------------------------------------------------------------------ value mapping

    /// <summary>
    /// <c>FaaliyetRaporSatir_T.SatirTuru</c>, one of fifteen Turkish slugs.
    /// <para>
    /// Returns <c>null</c> for anything unrecognised, so an unknown line is counted and dropped
    /// rather than filed under whichever member happened to be first. A report line means nothing
    /// without knowing which row of the report it is.
    /// </para>
    /// </summary>
    private static ActivityReportLineType? LineTypeOf(string? type)
        => Fold(type) switch
        {
            "KURUMBILGI" => ActivityReportLineType.OrganizationInfo,
            "FIRMABILGI" => ActivityReportLineType.CompanyInfo,
            "CALISANLAR" => ActivityReportLineType.Workers,
            "SUBEADET" => ActivityReportLineType.BranchCount,
            "SUBECALISANADET" => ActivityReportLineType.BranchWorkerCount,
            "ZIYARETADET" => ActivityReportLineType.VisitCount,
            "ZIYARETSAAT" => ActivityReportLineType.VisitHour,
            "ZIYARETTARIH" => ActivityReportLineType.VisitDate,
            "EGITIMALANPERSONELLER" => ActivityReportLineType.TrainedEmployees,
            "EGITIMEKSIKPERSONEL" => ActivityReportLineType.EmployeesMissingTraining,
            "PERSONELSAGLIKRAPORUDURUM" => ActivityReportLineType.EmployeeHealthReportStatus,
            "EKIPMANPERIYODIKKONTROL" => ActivityReportLineType.EquipmentPeriodicInspection,
            "MUAYENESIZEKIPMANLAR" => ActivityReportLineType.UnexaminedEquipments,
            "UYGUNSUZLUKLAR" => ActivityReportLineType.NonConformities,
            "OLAYLAR" => ActivityReportLineType.Incidents,
            _ => null,
        };

    /// <summary><c>Durum</c>: "Aktif" or "Pasif".</summary>
    private static CompanyCheckStatus CheckStatusOf(string? status)
        => Fold(status) switch
        {
            "AKTIF" => CompanyCheckStatus.Active,
            "PASIF" => CompanyCheckStatus.Cancelled,
            "TAMAMLANDI" => CompanyCheckStatus.Completed,
            "ONAYLANDI" => CompanyCheckStatus.Approved,
            _ => CompanyCheckStatus.Unspecified,
        };

    /// <summary><c>ISGRapor_T.PersonelTuru</c>: specialist, physician or other health personnel.</summary>
    private static StaffRole StaffRoleOf(string? role)
        => Fold(role) switch
        {
            "UZMAN" => StaffRole.OccupationalSafetySpecialist,
            "DOKTOR" => StaffRole.WorkplacePhysician,
            "DIGER PERSONEL" => StaffRole.OtherHealthPersonnel,
            _ => StaffRole.Unspecified,
        };

    /// <summary>
    /// <c>ISGRapor_T.GorevTuru</c>. All 7,319 rows say "Ice Grv." — an assignment into the
    /// organization's own workplaces, as opposed to one out to a customer's.
    /// </summary>
    private static AssignmentType AssignmentTypeOf(string? type)
    {
        var folded = Fold(type);

        if (folded is null)
        {
            return AssignmentType.Unspecified;
        }

        if (folded.StartsWith("ICE", StringComparison.Ordinal))
        {
            return AssignmentType.InboundAssignment;
        }

        return folded.StartsWith("DIS", StringComparison.Ordinal)
            ? AssignmentType.OutboundAssignment
            : AssignmentType.Unspecified;
    }

    /// <summary>
    /// <c>Kontrol_T.Periyot</c>: a unit letter and a count, "y1" for yearly and "a1" for monthly.
    /// <para>
    /// Six of the fifty-two rows say something else — "3a1", "s" — which fits no reading. Those
    /// fall back to once a year, which is the statutory minimum for a periodic check and so the
    /// safe end of the guess: a check due more often than recorded is a nuisance, one due less
    /// often is a missed obligation.
    /// </para>
    /// </summary>
    private static (PeriodUnit Unit, int Value) PeriodOf(string? period)
    {
        var folded = Fold(period);

        if (folded is null || folded.Length < 2)
        {
            return (PeriodUnit.Year, 1);
        }

        var unit = folded[0] switch
        {
            'Y' => PeriodUnit.Year,
            'A' => PeriodUnit.Month,
            'H' => PeriodUnit.Week,
            'G' => PeriodUnit.Day,
            _ => (PeriodUnit?)null,
        };

        if (unit is null || !int.TryParse(folded[1..], out var value) || value <= 0)
        {
            return (PeriodUnit.Year, 1);
        }

        return (unit.Value, value);
    }

    /// <summary>
    /// A date stored as text, "31-01-2019".
    /// <para>
    /// Anything that does not parse becomes null rather than a date nobody typed — and there is a
    /// lot that does not: "21-02-201822" is a typing slip, but the line-level column mostly holds
    /// periods ("01.01.2025 - 31.12.2025"), years, rules and pasted HTML. Those are kept verbatim
    /// in <c>DateText</c>; only a single unambiguous date reaches <c>Date</c>.
    /// </para>
    /// </summary>
    private static DateTime? DashDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string[] formats = ["dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "dd/MM/yyyy"];

        return DateTime.TryParseExact(
            value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
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
            note += $", {orphaned} SKIPPED";
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
            CommandTimeout = 600,
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
