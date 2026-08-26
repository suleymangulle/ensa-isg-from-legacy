using Ensa.Domain.Common;
using Ensa.Domain.Lookups;
using Ensa.Domain.Plans;
using Ensa.Domain.Plans.Navigations;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Plans;

/// <summary>
/// EF Core implementation of <see cref="IActivityRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class ActivityRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<Activity>(context, dataFilter), IActivityRepository
{
    /// <inheritdoc />
    /// <remarks>The total query count is constant (at most 5); the child activities are fetched with a single query.</remarks>
    public async Task<ActivityNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var activity = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (activity is null)
        {
            return null;
        }

        var navigation = new ActivityNavigation { Activity = activity };

        if (activity.ActivityGroupId is { } groupId)
        {
            navigation.ActivityGroup = await Context.Set<ActivityGroup>()
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        }

        if (activity.PeriodId is { } periodId)
        {
            navigation.Period = await Context.Set<Period>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken);
        }

        if (activity.ParentActivityId is { } parentId)
        {
            navigation.ParentActivity = await GetReadOnlyQueryable()
                .FirstOrDefaultAsync(a => a.Id == parentId, cancellationToken);
        }

        navigation.ChildActivities = await GetReadOnlyQueryable()
            .Where(a => a.ParentActivityId == id)
            .OrderBy(a => a.OrderNo)
            .ThenBy(a => a.ActivityName)
            .ToListAsync(cancellationToken);

        return navigation;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The global query filter already applies <c>TenantId == CurrentTenant.Id || TenantId == null</c>,
    /// so this method only <b>narrows</b> the result with the <paramref name="tenantId"/> parameter:
    /// activities specific to the target organization plus the host library rows shared by all
    /// organizations.
    /// </remarks>
    public Task<List<Activity>> GetDefaultActivitiesAsync(
        int? tenantId,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .Where(a => a.DefaultActivity
                        && a.IsActive
                        && (a.TenantId == tenantId || a.TenantId == null))
            .OrderBy(a => a.OrderNo)
            .ThenBy(a => a.ActivityName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<List<Activity>> GetChildActivitiesAsync(
        int parentActivityId,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .Where(a => a.ParentActivityId == parentActivityId)
            .OrderBy(a => a.OrderNo)
            .ThenBy(a => a.ActivityName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<List<ActivityPeriod>> GetPeriodsAsync(
        int activityId,
        CancellationToken cancellationToken = default)
        => Context.Set<ActivityPeriod>()
            .AsNoTracking()
            .Where(p => p.ActivityId == activityId)
            .OrderBy(p => p.PeriodId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<List<ActivityPeriod>> GetPeriodsAsync(
        IEnumerable<int> activityIds,
        CancellationToken cancellationToken = default)
    {
        List<int> ids = [.. activityIds.Where(id => id > 0).Distinct()];

        return ids.Count == 0
            ? Task.FromResult(new List<ActivityPeriod>())
            : Context.Set<ActivityPeriod>()
                .AsNoTracking()
                .Where(p => ids.Contains(p.ActivityId))
                .OrderBy(p => p.ActivityId)
                .ThenBy(p => p.PeriodId)
                .ToListAsync(cancellationToken);
    }
}
