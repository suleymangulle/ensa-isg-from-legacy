using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Trainings.Dtos;

/// <summary>A single row of the annual training plan list.</summary>
public class TrainingPlanListDto : EntityDto
{
    public int CompanyId { get; set; }

    /// <summary>Workplace name (resolved by the application service).</summary>
    public string? CompanyName { get; set; }

    public DateTime StartDate { get; set; }

    public string? DocumentNo { get; set; }

    public string? RevisionNo { get; set; }

    public DateTime PublicationDate { get; set; }

    public bool IsActive { get; set; }

    public bool Transferred { get; set; }

    /// <summary>Number of lines on the plan.</summary>
    public int LineCount { get; set; }
}

/// <summary>Annual training plan header (cover page).</summary>
public class TrainingPlanDto : FullAuditedEntityDto, IMultiTenantDto
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

    public bool IsActive { get; set; }

    /// <summary>Whether the plan has been pushed to the integrated external system (IBYS).</summary>
    public bool Transferred { get; set; }
}

/// <summary>A single line of a training plan.</summary>
public class TrainingPlanLineDto : EntityDto
{
    public int TrainingPlanId { get; set; }

    public int TrainingId { get; set; }

    public int? CompanyId { get; set; }

    public int DurationMinutes { get; set; }

    public int? Year { get; set; }

    public int? Month { get; set; }

    public PlanLineStatus Status { get; set; }

    public ApprovalStatus? ApprovalStatus { get; set; }

    /// <summary>Why the line was rejected; <c>null</c> unless it is in the rejected state.</summary>
    public string? RejectionReason { get; set; }

    public DateTime? PerformedDate { get; set; }

    public string? Source { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int? ForApprovalSenderUserId { get; set; }

    public int? ApproverUserId { get; set; }

    public DateTime? ForApprovalSendingDate { get; set; }

    public DateTime? ApprovalDate { get; set; }

    /// <summary>National id of an external instructor who has no user record.</summary>
    public string? InstructorNationalId { get; set; }

    public string? InstructorTitle { get; set; }

    public string? InstructorFullName { get; set; }

    public int? InstructorUserId { get; set; }

    public TrainingLocation? TrainingLocation { get; set; }

    public TrainingType? TrainingType { get; set; }

    public IbysSubmissionStatus IbysStatus { get; set; }

    public int? IbysQueryId { get; set; }

    public string? IbysStatusCode { get; set; }

    public string? IbysMessage { get; set; }

    public int? DocumentId { get; set; }
}

/// <summary>Input used to create a training plan header.</summary>
public class CreateTrainingPlanDto
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
}

/// <summary>Input used to update a training plan header.</summary>
public class UpdateTrainingPlanDto : CreateTrainingPlanDto
{
    public bool IsActive { get; set; } = true;

    public bool Transferred { get; set; }
}

/// <summary>Input used to add a line to a training plan.</summary>
public class CreateTrainingPlanLineDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A training must be selected.")]
    public int TrainingId { get; set; }

    [Range(0, 100000, ErrorMessage = "The duration must be between 0 and 100000 minutes.")]
    public int DurationMinutes { get; set; }

    [Range(2000, 2200)]
    public int? Year { get; set; }

    [Range(1, 12, ErrorMessage = "The month must be between 1 and 12.")]
    public int? Month { get; set; }

    [EnumDataType(typeof(PlanLineStatus))]
    public PlanLineStatus Status { get; set; } = PlanLineStatus.Planned;

    public DateTime? PerformedDate { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.ShortName)]
    public string? Source { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Description)]
    public string? Description { get; set; }

    [StringLength(EnsaDomainSharedConsts.MaxLengths.NationalId)]
    public string? InstructorNationalId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? InstructorTitle { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? InstructorFullName { get; set; }

    public int? InstructorUserId { get; set; }

    public TrainingLocation? TrainingLocation { get; set; }

    public TrainingType? TrainingType { get; set; }

    public int? DocumentId { get; set; }
}

/// <summary>Input used to update a training plan line.</summary>
public class UpdateTrainingPlanLineDto : CreateTrainingPlanLineDto
{
    public bool IsActive { get; set; } = true;
}

/// <summary>Filter for the training plan list.</summary>
/// <summary>
/// One row of the cross-plan training plan line list — the operational screen that shows what is
/// scheduled, for whom and when, without first picking a plan.
/// <para>Display names are resolved by the service with batched queries, never one query per row.</para>
/// </summary>
public class TrainingPlanLineListDto : EntityDto
{
    public int TrainingPlanId { get; set; }

    public string? CompanyName { get; set; }

    public string? TrainingName { get; set; }

    public int? Year { get; set; }

    public int? Month { get; set; }

    public int DurationMinutes { get; set; }

    public PlanLineStatus Status { get; set; }

    public ApprovalStatus? ApprovalStatus { get; set; }

    public DateTime? PerformedDate { get; set; }

    public string? InstructorFullName { get; set; }
}

/// <summary>Filter for the cross-plan line list.</summary>
public class GetTrainingPlanLineListInput : PagedAndSortedFilterDto
{
    public int? TrainingPlanId { get; set; }

    public int? CompanyId { get; set; }

    public int? TrainingId { get; set; }

    public int? Year { get; set; }

    public int? Month { get; set; }

    public PlanLineStatus? Status { get; set; }

    public ApprovalStatus? ApprovalStatus { get; set; }

    public bool? IsActive { get; set; }
}

public class GetTrainingPlanListInput : PagedAndSortedFilterDto
{
    public int? CompanyId { get; set; }

    public int? Year { get; set; }

    public int? SpecialistUserId { get; set; }

    public int? PhysicianUserId { get; set; }

    public bool? IsActive { get; set; }

    public bool? Transferred { get; set; }
}

/// <summary>Reason supplied when a plan line is rejected.</summary>
public class RejectPlanLineDto
{
    [Required(ErrorMessage = "A rejection reason is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Description)]
    public string Reason { get; set; } = string.Empty;
}
