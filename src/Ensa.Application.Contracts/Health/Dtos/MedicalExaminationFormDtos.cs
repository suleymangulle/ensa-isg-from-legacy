using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Health.Dtos;

/// <summary>
/// A single row of the medical examination form list.
/// <para>
/// <b>PRIVACY.</b> Occupational health records are special-category personal data
/// (GDPR art. 9 / KVKK art. 6). This DTO therefore carries <b>no clinical content</b>:
/// no anthropometry, no vital signs, no complaints, no findings, no laboratory
/// results and no free-text remarks. It exposes only the administrative envelope of
/// the record — employee, workplace, report type, dates, physician and the
/// fitness-for-work conclusion — which is what a list screen legitimately needs.
/// Clinical detail is available only through <c>GetAsync</c> / <c>GetWithNavigationAsync</c>,
/// which are guarded by the same permission but return a single, deliberately requested record.
/// </para>
/// </summary>
public class MedicalExaminationFormListDto : EntityDto
{
    public int CompanyEmployeeId { get; set; }

    /// <summary>Examined employee's display name (resolved by the application service).</summary>
    public string? EmployeeFullName { get; set; }

    public int? CompanyId { get; set; }

    /// <summary>Workplace name (resolved by the application service).</summary>
    public string? CompanyName { get; set; }

    public MedicalReportType ReportType { get; set; }

    public DateTime ExaminationDate { get; set; }

    public DateTime? ValidityDate { get; set; }

    public int? PhysicianUserId { get; set; }

    /// <summary>Examining physician's display name (resolved by the application service).</summary>
    public string? PhysicianFullName { get; set; }

    /// <summary>Fitness-for-work conclusion — an administrative outcome, not a diagnosis.</summary>
    public FitnessForWorkOpinion Opinion { get; set; }

    /// <summary>IBYS notification status of the record.</summary>
    public IbysSubmissionStatus IbysStatus { get; set; }
}

/// <summary>
/// Full medical examination form (header and conclusion).
/// <para>
/// <b>PRIVACY.</b> This DTO carries clinical data and must only ever be returned for a
/// single, explicitly requested record to a caller holding
/// <c>Ensa.MedicalExamination</c>. It must not be embedded in list or export payloads.
/// </para>
/// </summary>
public class MedicalExaminationFormDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int? CompanyId { get; set; }

    public int CompanyEmployeeId { get; set; }

    public MedicalReportType ReportType { get; set; }

    public DateTime ExaminationDate { get; set; }

    public DateTime? ValidityDate { get; set; }

    public int? PhysicianUserId { get; set; }

    // ---------------- Anthropometry / vital signs ----------------

    /// <summary>Height in centimetres.</summary>
    public int? HeightCm { get; set; }

    /// <summary>Weight in kilograms.</summary>
    public decimal? WeightKg { get; set; }

    /// <summary>Body mass index (kg/m²) — derived by <c>IHealthSurveillanceManager.CalculateBmi</c>.</summary>
    public decimal? BodyMassIndex { get; set; }

    public int? BloodPressureSystolic { get; set; }

    public int? BloodPressureDiastolic { get; set; }

    public int? PulseRate { get; set; }

    // ---------------- History and conclusion ----------------

    /// <summary>Chronic illness declared at the time of this examination.</summary>
    public string? ChronicIllnessDeclaration { get; set; }

    public FitnessForWorkOpinion Opinion { get; set; }

    public string? OpinionDescription { get; set; }

    public string? Recommendations { get; set; }

    public int? DocumentId { get; set; }

    // ---------------- IBYS ----------------

    public IbysSubmissionStatus IbysStatus { get; set; }

    public int? IbysQueryId { get; set; }

    public int? IbysStatusCode { get; set; }

    public string? IbysStatusMessage { get; set; }

    public string? IbysGroupCode { get; set; }

    public string? IbysOccupationCode { get; set; }

    public string? IbysWorkEnvironmentCodes { get; set; }

    public string? IbysWorkArrangementCodes { get; set; }

    public string? IbysWorkEquipmentCodes { get; set; }

    public string? Source { get; set; }
}

/// <summary>Input used to create a medical examination form.</summary>
public class CreateMedicalExaminationFormDto
{
    [Range(1, int.MaxValue, ErrorMessage = "An employee must be selected.")]
    public int CompanyEmployeeId { get; set; }

    public int? CompanyId { get; set; }

    [EnumDataType(typeof(MedicalReportType))]
    public MedicalReportType ReportType { get; set; } = MedicalReportType.Unspecified;

    [Required(ErrorMessage = "The examination date is required.")]
    public DateTime ExaminationDate { get; set; }

    /// <summary>
    /// Explicit validity end date. When omitted the statutory interval owned by
    /// <c>IHealthSurveillanceManager</c> is applied instead.
    /// </summary>
    public DateTime? ValidityDate { get; set; }

    public int? PhysicianUserId { get; set; }

    [Range(30, 260, ErrorMessage = "Height must be between 30 and 260 cm.")]
    public int? HeightCm { get; set; }

    [Range(1, 500, ErrorMessage = "Weight must be between 1 and 500 kg.")]
    public decimal? WeightKg { get; set; }

    [Range(30, 300)]
    public int? BloodPressureSystolic { get; set; }

    [Range(20, 200)]
    public int? BloodPressureDiastolic { get; set; }

    [Range(20, 300)]
    public int? PulseRate { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? ChronicIllnessDeclaration { get; set; }

    [EnumDataType(typeof(FitnessForWorkOpinion))]
    public FitnessForWorkOpinion Opinion { get; set; } = FitnessForWorkOpinion.Unspecified;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? OpinionDescription { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? Recommendations { get; set; }

    public int? DocumentId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? IbysOccupationCode { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Description)]
    public string? IbysWorkEnvironmentCodes { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Description)]
    public string? IbysWorkArrangementCodes { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Description)]
    public string? IbysWorkEquipmentCodes { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.ShortName)]
    public string? Source { get; set; }
}

/// <summary>Input used to update a medical examination form.</summary>
public class UpdateMedicalExaminationFormDto : CreateMedicalExaminationFormDto
{
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? IbysGroupCode { get; set; }
}

/// <summary>Filter for the medical examination form list.</summary>
public class GetMedicalExaminationFormListInput : PagedAndSortedFilterDto
{
    public int? CompanyId { get; set; }

    public int? CompanyEmployeeId { get; set; }

    public int? PhysicianUserId { get; set; }

    public MedicalReportType? ReportType { get; set; }

    public FitnessForWorkOpinion? Opinion { get; set; }

    public IbysSubmissionStatus? IbysStatus { get; set; }

    /// <summary>Lower bound for <c>ExaminationDate</c>.</summary>
    public DateTime? ExaminationDateFrom { get; set; }

    /// <summary>Upper bound for <c>ExaminationDate</c>.</summary>
    public DateTime? ExaminationDateTo { get; set; }
}
