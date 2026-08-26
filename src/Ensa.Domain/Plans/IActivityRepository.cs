using Ensa.Domain.Plans.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Plans;

/// <summary>
/// Module-specific repository contract for <see cref="Activity"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface IActivityRepository : IRepository<Activity>
{
    /// <summary>Loads the activity as a combined view with its group, period and parent/child activities.</summary>
    Task<ActivityNavigation?> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The list of default activities (<c>DefaultActivity == true</c>), used when generating a new
    /// work plan. (Legacy: the query filtered on <c>Aktivite_T.DefaultAktivite</c>.)
    /// </summary>
    Task<List<Activity>> GetDefaultActivitiesAsync(int? tenantId, CancellationToken cancellationToken = default);

    /// <summary>Returns the direct child activities of a parent activity.</summary>
    Task<List<Activity>> GetChildActivitiesAsync(int parentActivityId, CancellationToken cancellationToken = default);

    /// <summary>Returns the period mappings defined for an activity.</summary>
    Task<List<ActivityPeriod>> GetPeriodsAsync(int activityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the period mappings of several activities in a single query.
    /// <para>
    /// The single-activity overload above invites a loop, and the only caller that matters -
    /// generating a plan's default lines - needs the periods of the whole default activity set.
    /// Calling it once per activity is the classic N+1, so that path uses this instead.
    /// </para>
    /// </summary>
    Task<List<ActivityPeriod>> GetPeriodsAsync(
        IEnumerable<int> activityIds,
        CancellationToken cancellationToken = default);
}
