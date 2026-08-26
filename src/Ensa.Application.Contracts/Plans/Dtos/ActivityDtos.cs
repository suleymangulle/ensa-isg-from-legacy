using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Plans.Dtos;

/// <summary>
/// A single row of the activity catalogue list.
/// <para>
/// <b>TENANCY.</b> The activity catalogue is a mixed host/tenant catalogue:
/// <c>TenantId == null</c> marks a shared, host-level entry that every organisation sees,
/// while a non-null value marks an entry private to that organisation. The split is applied
/// by the global query filter in <c>EnsaDbContext</c> — no manual tenant predicate is added
/// anywhere in this module.
/// </para>
/// </summary>
public class ActivityListDto : EntityDto
{
    public string ActivityName { get; set; } = string.Empty;

    public string? ActivityCode { get; set; }

    public int? ActivityGroupId { get; set; }

    public ActivityType ActivityType { get; set; }

    public int? ParentActivityId { get; set; }

    public int? PeriodId { get; set; }

    public bool DefaultActivity { get; set; }

    public bool IsActive { get; set; }

    public int? OrderNo { get; set; }

    /// <summary><c>null</c> means the entry is shared across all organisations.</summary>
    public int? TenantId { get; set; }
}

/// <summary>Activity / document / revision catalogue entry.</summary>
public class ActivityDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    /// <summary>Parent activity in the hierarchy.</summary>
    public int? ParentActivityId { get; set; }

    public string? ActivityCode { get; set; }

    public string ActivityName { get; set; } = string.Empty;

    public int? ActivityGroupId { get; set; }

    public ActivityType ActivityType { get; set; }

    /// <summary>Whether the activity is placed on a new work plan by default.</summary>
    public bool DefaultActivity { get; set; }

    /// <summary>How many times a year the activity is planned by default.</summary>
    public int DefaultCount { get; set; }

    /// <summary>Month offset from the start of the plan year.</summary>
    public int DefaultStartMonthOffset { get; set; }

    /// <summary>Minimum employee count that makes the activity mandatory.</summary>
    public int DefaultElementCondition { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Recurrence period of the activity.</summary>
    public int? PeriodId { get; set; }

    /// <summary>Table name of the polymorphic reference, for example "RiskAnalizRaporu".</summary>
    public string? RelatedTable { get; set; }

    /// <summary>Record id of the polymorphic reference.</summary>
    public int? RelationId { get; set; }

    public int? OrderNo { get; set; }
}

/// <summary>Input used to create an activity catalogue entry.</summary>
public class CreateActivityDto
{
    [Required(ErrorMessage = "The activity name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string ActivityName { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? ActivityCode { get; set; }

    public int? ParentActivityId { get; set; }

    public int? ActivityGroupId { get; set; }

    [EnumDataType(typeof(ActivityType))]
    public ActivityType ActivityType { get; set; } = ActivityType.Activity;

    public bool DefaultActivity { get; set; }

    [Range(0, 12, ErrorMessage = "The default count must be between 0 and 12.")]
    public int DefaultCount { get; set; }

    [Range(0, 11, ErrorMessage = "The month offset must be between 0 and 11.")]
    public int DefaultStartMonthOffset { get; set; }

    [Range(0, int.MaxValue)]
    public int DefaultElementCondition { get; set; }

    public int? PeriodId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? RelatedTable { get; set; }

    public int? RelationId { get; set; }

    [Range(0, int.MaxValue)]
    public int? OrderNo { get; set; }
}

/// <summary>Input used to update an activity catalogue entry.</summary>
public class UpdateActivityDto : CreateActivityDto
{
    public bool IsActive { get; set; } = true;
}

/// <summary>Filter for the activity catalogue list.</summary>
public class GetActivityListInput : PagedAndSortedFilterDto
{
    public int? ActivityGroupId { get; set; }

    public ActivityType? ActivityType { get; set; }

    public int? ParentActivityId { get; set; }

    public int? PeriodId { get; set; }

    public bool? DefaultActivity { get; set; }

    public bool? IsActive { get; set; }
}
