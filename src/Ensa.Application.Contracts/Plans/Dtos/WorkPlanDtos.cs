using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Plans.Dtos;

/// <summary>A single row of the annual OHS work plan list.</summary>
public class WorkPlanListDto : EntityDto
{
    public int CompanyId { get; set; }

    /// <summary>Workplace name (resolved by the application service).</summary>
    public string? CompanyName { get; set; }

    public DateTime StartDate { get; set; }

    public string? DocumentNo { get; set; }

    public string? RevisionNo { get; set; }

    public DateTime PublicationDate { get; set; }

    public bool IsActive { get; set; }

    public bool IsTransferred { get; set; }

    /// <summary>Number of lines on the plan.</summary>
    public int LineCount { get; set; }
}

/// <summary>Annual occupational health and safety work plan header (cover page).</summary>
public class WorkPlanDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int CompanyId { get; set; }

    public DateTime StartDate { get; set; }

    public string? RevisionNo { get; set; }

    public DateTime RevisionDate { get; set; }

    public string? DocumentNo { get; set; }

    public DateTime PublicationDate { get; set; }

    /// <summary>Occupational safety specialist who drew up the plan.</summary>
    public int? SpecialistUserId { get; set; }

    /// <summary>Occupational physician who drew up the plan.</summary>
    public int? PhysicianUserId { get; set; }

    public int? ApproverUserId { get; set; }

    /// <summary>Checklist definition the plan is bound to.</summary>
    public int? ControlItemListId { get; set; }

    public bool IsActive { get; set; }

    public bool IsTransferred { get; set; }

    /// <summary>Previous year's revision of this plan.</summary>
    public int? PreviousPlanId { get; set; }
}

/// <summary>A single line of a work plan.</summary>
public class WorkPlanLineDto : EntityDto
{
    public int WorkPlanId { get; set; }

    public int ActivityId { get; set; }

    public int? PeriodId { get; set; }

    public int Year { get; set; }

    public int? Month { get; set; }

    public PlanLineStatus? Status { get; set; }

    public DateTime? PerformedDate { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int? PreviousLineId { get; set; }

    public ApprovalStatus? ApprovalStatus { get; set; }

    /// <summary>Why the line was rejected; <c>null</c> unless it is in the rejected state.</summary>
    public string? RejectionReason { get; set; }

    public int? ForApprovalSenderUserId { get; set; }

    public int? ApproverUserId { get; set; }

    public DateTime? ForApprovalSendingDate { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public int CompanyId { get; set; }

    /// <summary>National id of an external instructor who has no user record.</summary>
    public string? InstructorNationalId { get; set; }

    public int? InstructorUserId { get; set; }

    public int? DocumentId { get; set; }
}

/// <summary>Input used to create a work plan header.</summary>
public class CreateWorkPlanDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A workplace must be selected.")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "The plan start date is required.")]
    public DateTime StartDate { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? RevisionNo { get; set; }

    public DateTime RevisionDate { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? DocumentNo { get; set; }

    public DateTime PublicationDate { get; set; }

    public int? SpecialistUserId { get; set; }

    public int? PhysicianUserId { get; set; }

    public int? ApproverUserId { get; set; }

    public int? ControlItemListId { get; set; }

    public int? PreviousPlanId { get; set; }
}

/// <summary>Input used to update a work plan header.</summary>
public class UpdateWorkPlanDto : CreateWorkPlanDto
{
    public bool IsActive { get; set; } = true;

    public bool IsTransferred { get; set; }
}

/// <summary>Input used to add a line to a work plan.</summary>
public class CreateWorkPlanLineDto
{
    [Range(1, int.MaxValue, ErrorMessage = "An activity must be selected.")]
    public int ActivityId { get; set; }

    public int? PeriodId { get; set; }

    [Range(2000, 2200, ErrorMessage = "The year must be between 2000 and 2200.")]
    public int Year { get; set; }

    [Range(1, 12, ErrorMessage = "The month must be between 1 and 12.")]
    public int? Month { get; set; }

    [EnumDataType(typeof(PlanLineStatus))]
    public PlanLineStatus? Status { get; set; } = PlanLineStatus.Planned;

    public DateTime? PerformedDate { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Description)]
    public string? Description { get; set; }

    [StringLength(EnsaDomainSharedConsts.MaxLengths.NationalId)]
    public string? InstructorNationalId { get; set; }

    public int? InstructorUserId { get; set; }

    public int? DocumentId { get; set; }
}

/// <summary>Input used to update a work plan line.</summary>
public class UpdateWorkPlanLineDto : CreateWorkPlanLineDto
{
    public bool IsActive { get; set; } = true;
}

/// <summary>Filter for the work plan list.</summary>
public class GetWorkPlanListInput : PagedAndSortedFilterDto
{
    public int? CompanyId { get; set; }

    public int? Year { get; set; }

    public int? SpecialistUserId { get; set; }

    public int? PhysicianUserId { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsTransferred { get; set; }
}

/// <summary>Completion figures of a work plan.</summary>
public class WorkPlanCompletionDto
{
    public int WorkPlanId { get; set; }

    /// <summary>Share of lines that reached <c>Completed</c>, between 0 and 1.</summary>
    public double CompletionRate { get; set; }

    /// <summary>The same share expressed as a percentage, rounded to two decimals.</summary>
    public double CompletionPercentage { get; set; }
}

/// <summary>Reason supplied when a work plan line is rejected.</summary>
public class RejectWorkPlanLineDto
{
    [Required(ErrorMessage = "A rejection reason is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Description)]
    public string Reason { get; set; } = string.Empty;
}
