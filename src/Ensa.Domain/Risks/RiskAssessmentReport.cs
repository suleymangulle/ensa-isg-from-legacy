using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// The HEADER record of a risk assessment report. (Legacy: <c>RiskAnalizRaporu_T</c>)
/// <para>
/// The legacy table was an enormous one, with close to seventy flat columns. After normalization
/// only the header fields remain here; the repeated boolean and CSV column groups moved into child
/// tables:
/// <list type="bullet">
/// <item><c>TMK*</c> (10 bool) → <see cref="RiskAssessmentExposedGroup"/></item>
/// <item><c>MKO*</c> (7 bool) → <see cref="RiskAssessmentControlMeasure"/></item>
/// <item><c>IO*</c> (7 bool) → <see cref="RiskAssessmentImprovementAction"/></item>
/// <item><c>FemaleWorker/ElderlyWorker/ChildWorker/DisabledWorker</c> → <see cref="RiskAssessmentProtectedGroup"/></item>
/// <item><c>WorkplaceWorkerRepresentative/SupportStaff/InfoOwnerWorkers</c> (CSV string) → <see cref="RiskAssessmentParticipant"/></item>
/// <item>four separate <c>Risk*Record_T</c> tables → <see cref="RiskAssessmentHistoryRecord"/></item>
/// </list>
/// </para>
/// </summary>
public class RiskAssessmentReport : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>Display name of the report. (Legacy: <c>RaporAdi</c>)</summary>
    public string ReportName { get; set; } = string.Empty;

    /// <summary>The company the report belongs to. FK → <c>Company.Id</c>.</summary>
    public int CompanyId { get; set; }

    // ---- Workplace details as of the report date, copied from the company record ----

    public string WorkplaceTitle { get; set; } = string.Empty;

    public string BusinessActivity { get; set; } = string.Empty;

    public string WorkplaceAddress { get; set; } = string.Empty;

    public string WorkplaceTelefonu { get; set; } = string.Empty;

    /// <summary>Legacy held this as a string ("AZ TEHLİKELİ" and so on); it was converted to an enum.</summary>
    public HazardClass HazardClass { get; set; }

    /// <summary>Free-text listing of the workplace departments. (Legacy: <c>IsyeriBolumleri</c>)</summary>
    public string? WorkplaceDepartments { get; set; }

    public string? MachinesVeEquipments { get; set; }

    public string? HazardousArticles { get; set; }

    public string? WasteOperations { get; set; }

    // ---- Tarihler ----

    /// <summary>The date the risk assessment was carried out.</summary>
    public DateTime PerformedDate { get; set; }

    /// <summary>
    /// End of the report's validity, computed from the hazard class by
    /// <c>RiskAssessmentManager.CalculateValidUntilDate</c>.
    /// </summary>
    public DateTime ValidityDate { get; set; }

    /// <summary>Revision date, when the report has been revised.</summary>
    public DateTime? RevisionDate { get; set; }

    // ---- Signatories ----

    /// <summary>Full name of the employer or their representative. (Legacy: <c>Isveren</c> string)</summary>
    public string? Employer { get; set; }

    /// <summary>
    /// The OHS specialist user. FK → <c>User.Id</c>.
    /// Legacy had only the free-text <c>Specialist</c> column; the FK was added.
    /// </summary>
    public int? SpecialistUserId { get; set; }

    /// <summary>
    /// The counterpart of the legacy <c>Specialist</c> string column. It is kept both for old records
    /// whose FK could not be resolved and to preserve the name as it appeared on the report.
    /// </summary>
    public string? SpecialistFullName { get; set; }

    /// <summary>The workplace physician user. FK → <c>User.Id</c>.</summary>
    public int? PhysicianUserId { get; set; }

    /// <summary>The counterpart of the legacy <c>Physician</c> string column.</summary>
    public string? PhysicianFullName { get; set; }

    /// <summary>Total number of workers as of the report date.</summary>
    public int WorkerCount { get; set; }

    /// <summary>Legacy: <c>RaporMetodu</c> string ("finekinney"/"matris").</summary>
    public RiskAssessmentMethod ReportMethod { get; set; } = RiskAssessmentMethod.FineKinney;

    /// <summary>Legacy: <c>KayitDurumu</c> string.</summary>
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;

    // NOTE: legacy `SilindiMi` → `IsDeleted` on the base class;
    //       `KurumId` → `TenantId` on the base class;
    //       `EklemeTarihi/GuncellemeTarihi/EkleyenKullanici/GuncelleyenKulanici` → the audit fields.
}
