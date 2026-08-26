using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Reports.Dtos;

// ------------------------------------------------------------- Activity report

/// <summary>Activity report list row.</summary>
public class ActivityReportListDto : EntityDto
{
    public int CompanyId { get; set; }

    /// <summary>Resolved by the application service with one batched query per page.</summary>
    public string? CompanyName { get; set; }
    public ActivityReportType ReportType { get; set; }
    public string ReportName { get; set; } = string.Empty;
    public DateTime ReportStart { get; set; }
    public DateTime ReportEnd { get; set; }
}

/// <summary>Activity report header.</summary>
public class ActivityReportDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int CompanyId { get; set; }
    public ActivityReportType ReportType { get; set; }
    public string ReportName { get; set; } = string.Empty;
    public DateTime ReportStart { get; set; }
    public DateTime ReportEnd { get; set; }
}

/// <summary>One typed data row of an activity report.</summary>
public class ActivityReportLineDto : CreationAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int ActivityReportId { get; set; }
    public ActivityReportLineType LineType { get; set; }
    public string? Text { get; set; }
    public string? Value1 { get; set; }
    public string? Value2 { get; set; }
    public string? Value3 { get; set; }
    public int OrderNo { get; set; }
}

/// <summary>Activity report creation input.</summary>
public class CreateActivityReportDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A workplace must be selected.")]
    public int CompanyId { get; set; }

    public ActivityReportType ReportType { get; set; } = ActivityReportType.MonthlyActivityReport;

    [Required(ErrorMessage = "The report name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string ReportName { get; set; } = string.Empty;

    [Required(ErrorMessage = "The period start is required.")]
    public DateTime ReportStart { get; set; }

    [Required(ErrorMessage = "The period end is required.")]
    public DateTime ReportEnd { get; set; }
}

/// <summary>Activity report update input.</summary>
public class UpdateActivityReportDto : CreateActivityReportDto;

/// <summary>Activity report line input.</summary>
public class CreateActivityReportLineDto
{
    public ActivityReportLineType LineType { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Text { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Description)]
    public string? Value1 { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Description)]
    public string? Value2 { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Description)]
    public string? Value3 { get; set; }

    /// <summary>Display order. Zero means "append to the end".</summary>
    public int OrderNo { get; set; }
}

/// <summary>Activity report line update input.</summary>
public class UpdateActivityReportLineDto : CreateActivityReportLineDto;

/// <summary>Activity report list filter.</summary>
public class GetActivityReportListInput : PagedAndSortedFilterDto
{
    public int? CompanyId { get; set; }
    public ActivityReportType? ReportType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

// ------------------------------------------------- Year-end review report

/// <summary>Year-end review report list row.</summary>
public class YearEndReviewReportListDto : EntityDto
{
    public string ReportTitle { get; set; } = string.Empty;
    public int CompanyId { get; set; }

    /// <summary>Resolved by the application service with one batched query per page.</summary>
    public string? CompanyName { get; set; }
    public DateTime ReportDate { get; set; }
    public string? SpecialistFullName { get; set; }
    public string? PhysicianFullName { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Year-end review report header.</summary>
public class YearEndReviewReportDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string ReportTitle { get; set; } = string.Empty;
    public int CompanyId { get; set; }

    public int? MaleWorker { get; set; }
    public int? FemaleWorker { get; set; }
    public int? ChildWorker { get; set; }
    public int? YoungWorker { get; set; }

    public DateTime ReportDate { get; set; }

    public int? SpecialistUserId { get; set; }

    /// <summary>Name captured at authoring time so the report text survives user deletion.</summary>
    public string? SpecialistFullName { get; set; }

    public int? PhysicianUserId { get; set; }
    public string? PhysicianFullName { get; set; }

    public string? DeputyFullName { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>One work item of a year-end review report. Items form a tree via <see cref="ParentLineId"/>.</summary>
public class YearEndReviewLineDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int YearEndReviewReportId { get; set; }
    public int OrderNo { get; set; }
    public DateTime? Date { get; set; }
    public string? Work { get; set; }
    public string? PersonVeTitle { get; set; }
    public string? RepeatCount { get; set; }
    public string? UsedMethod { get; set; }
    public string? ResultVeComment { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Parent work item. <c>null</c> marks a root-level item.</summary>
    public int? ParentLineId { get; set; }
}

/// <summary>Year-end review report creation input.</summary>
public class CreateYearEndReviewReportDto
{
    [Required(ErrorMessage = "The report title is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string ReportTitle { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "A workplace must be selected.")]
    public int CompanyId { get; set; }

    [Range(0, 1000000, ErrorMessage = "The head count is out of range.")]
    public int? MaleWorker { get; set; }

    [Range(0, 1000000, ErrorMessage = "The head count is out of range.")]
    public int? FemaleWorker { get; set; }

    [Range(0, 1000000, ErrorMessage = "The head count is out of range.")]
    public int? ChildWorker { get; set; }

    [Range(0, 1000000, ErrorMessage = "The head count is out of range.")]
    public int? YoungWorker { get; set; }

    [Required(ErrorMessage = "The report date is required.")]
    public DateTime ReportDate { get; set; }

    public int? SpecialistUserId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? SpecialistFullName { get; set; }

    public int? PhysicianUserId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? PhysicianFullName { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? DeputyFullName { get; set; }
}

/// <summary>Year-end review report update input.</summary>
public class UpdateYearEndReviewReportDto : CreateYearEndReviewReportDto
{
    public bool IsActive { get; set; } = true;
}

/// <summary>Year-end review work item input.</summary>
public class CreateYearEndReviewLineDto
{
    /// <summary>Parent work item; leave <c>null</c> for a root-level item.</summary>
    public int? ParentLineId { get; set; }

    /// <summary>Display order among siblings. Zero means "append to the end".</summary>
    public int OrderNo { get; set; }

    public DateTime? Date { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Work { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Description)]
    public string? PersonVeTitle { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? RepeatCount { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? UsedMethod { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? ResultVeComment { get; set; }
}

/// <summary>Year-end review work item update input.</summary>
public class UpdateYearEndReviewLineDto : CreateYearEndReviewLineDto
{
    public bool IsActive { get; set; } = true;
}

/// <summary>Year-end review report list filter.</summary>
public class GetYearEndReviewReportListInput : PagedAndSortedFilterDto
{
    public int? CompanyId { get; set; }
    public int? SpecialistUserId { get; set; }
    public int? PhysicianUserId { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

// ------------------------------------------------------------------ OHS report

/// <summary>OHS service-time report list row.</summary>
public class OhsReportListDto : EntityDto
{
    public int OfficeId { get; set; }
    public string NationalId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public StaffRole StaffRole { get; set; }
    public AssignmentType DutyType { get; set; }
    public int TotalMinutes { get; set; }
    public int UsedMonthlyMinutes { get; set; }
    public DateTime CreationTime { get; set; }
}

/// <summary>
/// OHS service-time and assignment summary of one staff member for one period.
/// </summary>
public class OhsReportDto : CreationAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int OfficeId { get; set; }

    /// <summary>The archive detail record the report was produced from.</summary>
    public int ModuleArchiveDetailId { get; set; }

    public string NationalId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public StaffRole StaffRole { get; set; }
    public AssignmentType DutyType { get; set; }

    public int TotalMonthlyFazlaOvertimeDuration { get; set; }
    public int TotalMinutes { get; set; }
    public int UsedMonthlyMinutes { get; set; }
}

/// <summary>One hazard-class bucket of an OHS report.</summary>
public class OhsReportHazardClassBreakdownDto
{
    public HazardClass HazardClass { get; set; }

    /// <summary>Number of workplaces in this hazard class covered by the report.</summary>
    public int CompanyCount { get; set; }
}

/// <summary>OHS report list filter.</summary>
public class GetOhsReportListInput : PagedAndSortedFilterDto
{
    public int? OfficeId { get; set; }
    public StaffRole? StaffRole { get; set; }
    public AssignmentType? DutyType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
