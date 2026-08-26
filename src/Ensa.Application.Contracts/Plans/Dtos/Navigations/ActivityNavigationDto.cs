using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Plans.Dtos.Navigations;

/// <summary>
/// Combined view of an activity: the catalogue entry, its group, its recurrence period,
/// its parent and its direct children.
/// </summary>
public class ActivityNavigationDto : NavigationDto
{
    public ActivityDto Activity { get; set; } = null!;

    /// <summary>Activity group (category), reduced to a lookup.</summary>
    public LookupDto? ActivityGroup { get; set; }

    /// <summary>Recurrence period, reduced to a lookup.</summary>
    public LookupDto? Period { get; set; }

    /// <summary>Parent activity in the hierarchy, reduced to a lookup.</summary>
    public LookupDto? ParentActivity { get; set; }

    /// <summary>Direct children of this activity, reduced to lookups.</summary>
    public List<LookupDto> ChildActivities { get; set; } = [];
}
