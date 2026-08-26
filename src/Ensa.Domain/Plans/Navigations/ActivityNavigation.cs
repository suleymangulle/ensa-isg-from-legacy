using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Lookups;

namespace Ensa.Domain.Plans.Navigations;

/// <summary>
/// Combined view of an activity with its group, period, parent activity and child activities.
/// <para>
/// <c>[NotMapped]</c> — NEVER a <c>DbSet</c>, never added to <c>ModelBuilder</c>;
/// populated in the repository layer through an <c>IQueryable</c> join and projection.
/// </para>
/// </summary>
[NotMapped]
public class ActivityNavigation : NavigationEntity<Activity>
{
    public Activity Activity
    {
        get => Entity;
        set => Entity = value;
    }

    public ActivityGroup? ActivityGroup { get; set; }

    public Period? Period { get; set; }

    /// <summary>Parent activity in the hierarchy (<see cref="Activity.ParentActivityId"/>).</summary>
    public Activity? ParentActivity { get; set; }

    /// <summary>The child activities under this activity.</summary>
    public List<Activity> ChildActivities { get; set; } = [];
}
