using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Risks.Dtos;

/// <summary>Risk assessment report grid row.</summary>
public class RiskAssessmentReportListDto : EntityDto
{
    public string ReportName { get; set; } = string.Empty;
    public int CompanyId { get; set; }

    /// <summary>Resolved by the application service with a single batched company lookup.</summary>
    public string? CompanyName { get; set; }

    public HazardClass HazardClass { get; set; }
    public RiskAssessmentMethod ReportMethod { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }

    public DateTime PerformedDate { get; set; }
    public DateTime ValidityDate { get; set; }
    public DateTime? RevisionDate { get; set; }

    public int WorkerCount { get; set; }

    public string? SpecialistFullName { get; set; }
    public string? PhysicianFullName { get; set; }

    /// <summary>True when <see cref="ValidityDate"/> is already behind the reference date.</summary>
    public bool IsExpired { get; set; }

    /// <summary>Days left until <see cref="ValidityDate"/>; negative when already expired.</summary>
    public int RemainingDays { get; set; }
}

/// <summary>Risk assessment report header detail.</summary>
public class RiskAssessmentReportDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string ReportName { get; set; } = string.Empty;
    public int CompanyId { get; set; }

    // ---- Workplace identity snapshot ----
    public string WorkplaceTitle { get; set; } = string.Empty;
    public string BusinessActivity { get; set; } = string.Empty;
    public string WorkplaceAddress { get; set; } = string.Empty;
    public string WorkplacePhoneNumber { get; set; } = string.Empty;
    public HazardClass HazardClass { get; set; }
    public string? WorkplaceDepartments { get; set; }
    public string? MachineryAndEquipment { get; set; }
    public string? HazardousArticles { get; set; }
    public string? WasteOperations { get; set; }

    // ---- Dates ----
    public DateTime PerformedDate { get; set; }

    /// <summary>Computed by <c>IRiskAssessmentManager.CalculateValidUntilDate</c>; never supplied by the client.</summary>
    public DateTime ValidityDate { get; set; }

    public DateTime? RevisionDate { get; set; }

    // ---- Signatories ----
    public string? Employer { get; set; }
    public int? SpecialistUserId { get; set; }
    public string? SpecialistFullName { get; set; }
    public int? PhysicianUserId { get; set; }
    public string? PhysicianFullName { get; set; }

    public int WorkerCount { get; set; }
    public RiskAssessmentMethod ReportMethod { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }

    /// <summary>Whether the report was still valid at the moment it was read.</summary>
    public bool IsValid { get; set; }
}

/// <summary>Risk assessment report creation input.</summary>
public class CreateRiskAssessmentReportDto
{
    [Required(ErrorMessage = "The report name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string ReportName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "A company must be selected.")]
    public int CompanyId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string WorkplaceTitle { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string BusinessActivity { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Address)]
    public string WorkplaceAddress { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string WorkplacePhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Drives the validity period (2 / 4 / 6 years).
    /// <c>Unspecified</c> is rejected by <c>IRiskAssessmentManager.CalculateValidUntilDate</c>.
    /// </summary>
    public HazardClass HazardClass { get; set; } = HazardClass.Unspecified;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? WorkplaceDepartments { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? MachineryAndEquipment { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? HazardousArticles { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? WasteOperations { get; set; }

    [Required(ErrorMessage = "The assessment date is required.")]
    public DateTime PerformedDate { get; set; }

    public DateTime? RevisionDate { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? Employer { get; set; }

    public int? SpecialistUserId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? SpecialistFullName { get; set; }

    public int? PhysicianUserId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? PhysicianFullName { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "The worker count cannot be negative.")]
    public int WorkerCount { get; set; }

    public RiskAssessmentMethod ReportMethod { get; set; } = RiskAssessmentMethod.FineKinney;
}

/// <summary>Risk assessment report update input.</summary>
public class UpdateRiskAssessmentReportDto : CreateRiskAssessmentReportDto
{
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
}

/// <summary>Risk assessment report list filter.</summary>
public class GetRiskAssessmentReportListInput : PagedAndSortedFilterDto
{
    public int? CompanyId { get; set; }

    public ApprovalStatus? ApprovalStatus { get; set; }

    /// <summary>Filters on <c>RiskAssessmentReport.ReportMethod</c>.</summary>
    public RiskAssessmentMethod? AssessmentMethod { get; set; }

    public HazardClass? HazardClass { get; set; }

    public int? SpecialistUserId { get; set; }

    /// <summary>Assessment date lower bound (inclusive).</summary>
    public DateTime? PerformedFrom { get; set; }

    /// <summary>Assessment date upper bound (inclusive).</summary>
    public DateTime? PerformedTo { get; set; }

    /// <summary>
    /// When true, only reports whose validity ends within <see cref="ExpiringWithinDays"/> days
    /// are returned (already-expired ones included).
    /// </summary>
    public bool OnlyExpiringSoon { get; set; }

    /// <summary>Look-ahead window used together with <see cref="OnlyExpiringSoon"/>.</summary>
    [Range(0, 3650)]
    public int ExpiringWithinDays { get; set; } = 30;
}

/// <summary>A single identified hazard line of the report.</summary>
public class IdentifiedHazardDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int RiskAssessmentReportId { get; set; }
    public int? HazardCategoryId { get; set; }
    public int? HazardId { get; set; }

    public string HazardTag { get; set; } = string.Empty;
    public string? ActivityDescription { get; set; }
    public string? OwnerPerson { get; set; }
    public string? RiskTag { get; set; }
    public string? Measure { get; set; }

    // ---- Pre-control assessment ----
    public decimal Likelihood { get; set; }
    public decimal Severity { get; set; }
    public decimal Frequency { get; set; }

    /// <summary>Computed by <c>IRiskAssessmentManager</c>; read-only for clients.</summary>
    public decimal RiskScore { get; set; }

    /// <summary>Level derived from <see cref="RiskScore"/> via <c>IRiskAssessmentManager.DetermineLevel</c>.</summary>
    public RiskLevel RiskLevel { get; set; }

    public string? Comment { get; set; }

    // ---- Post-control (residual) assessment ----
    public decimal? ResidualLikelihood { get; set; }
    public decimal? ResidualSeverity { get; set; }
    public decimal? ResidualFrequency { get; set; }

    /// <summary>Computed by <c>IRiskAssessmentManager</c>; <c>null</c> until every residual input is supplied.</summary>
    public decimal? ResidualRiskScore { get; set; }

    /// <summary>Level derived from <see cref="ResidualRiskScore"/>.</summary>
    public RiskLevel ResidualRiskLevel { get; set; }

    public string? ResidualComment { get; set; }

    public HazardSourceType SourceType { get; set; }
    public int? SourceId { get; set; }
    public int? DocumentId { get; set; }
    public DateTime? DeadlineDate { get; set; }
}

/// <summary>Identified hazard creation input.</summary>
public class CreateIdentifiedHazardDto
{
    public int? HazardCategoryId { get; set; }

    /// <summary>Source record in the hazard library when the line was picked from it.</summary>
    public int? HazardId { get; set; }

    [Required(ErrorMessage = "The hazard description is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string HazardTag { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? ActivityDescription { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? OwnerPerson { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? RiskTag { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Measure { get; set; }

    [Range(0.0, 10000.0, ErrorMessage = "The likelihood value is out of range.")]
    public decimal Likelihood { get; set; }

    [Range(0.0, 10000.0, ErrorMessage = "The severity value is out of range.")]
    public decimal Severity { get; set; }

    /// <summary>Only used by the Fine-Kinney method.</summary>
    [Range(0.0, 10000.0, ErrorMessage = "The frequency value is out of range.")]
    public decimal Frequency { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Comment { get; set; }

    public decimal? ResidualLikelihood { get; set; }
    public decimal? ResidualSeverity { get; set; }
    public decimal? ResidualFrequency { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? ResidualComment { get; set; }

    public HazardSourceType SourceType { get; set; } = HazardSourceType.Manual;
    public int? SourceId { get; set; }
    public int? DocumentId { get; set; }
    public DateTime? DeadlineDate { get; set; }
}

/// <summary>Identified hazard update input.</summary>
public class UpdateIdentifiedHazardDto : CreateIdentifiedHazardDto;

/// <summary>A control measure attached to an identified hazard.</summary>
public class ControlMeasureDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int IdentifiedHazardId { get; set; }
    public string Measure { get; set; } = string.Empty;
    public DateTime? DeadlineDate { get; set; }
    public int? OwnerCompanyEmployeeId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletionDate { get; set; }
}

/// <summary>Control measure creation input.</summary>
public class CreateControlMeasureDto
{
    [Required(ErrorMessage = "The control measure text is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string Measure { get; set; } = string.Empty;

    public DateTime? DeadlineDate { get; set; }

    public int? OwnerCompanyEmployeeId { get; set; }
}

/// <summary>Exposed person group flagged on the report header.</summary>
public class RiskAssessmentExposedGroupDto : EntityDto
{
    public int RiskAssessmentReportId { get; set; }
    public ExposedPersonGroup Group { get; set; }
}

/// <summary>Existing control measure flagged on the report header.</summary>
public class RiskAssessmentControlMeasureDto : EntityDto
{
    public int RiskAssessmentReportId { get; set; }
    public ExistingControlMeasure Measure { get; set; }
}

/// <summary>Improvement recommendation flagged on the report header.</summary>
public class RiskAssessmentImprovementActionDto : EntityDto
{
    public int RiskAssessmentReportId { get; set; }
    public ImprovementAction Recommendation { get; set; }
}

/// <summary>Vulnerable worker group present at the workplace.</summary>
public class RiskAssessmentProtectedGroupDto : EntityDto
{
    public int RiskAssessmentReportId { get; set; }
    public VulnerableWorkerGroup Group { get; set; }
    public int? Number { get; set; }
}

/// <summary>Member of the risk assessment team.</summary>
public class RiskAssessmentParticipantDto : EntityDto
{
    public int RiskAssessmentReportId { get; set; }
    public ReportParticipantType ParticipantType { get; set; }
    public int? CompanyEmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
}

/// <summary>Past accident / occupational disease / near-miss record of the workplace.</summary>
public class RiskAssessmentHistoryRecordDto : EntityDto
{
    public int RiskAssessmentReportId { get; set; }
    public RiskHistoryRecordType RecordType { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
}
