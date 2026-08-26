using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Plans;
using Ensa.Application.Contracts.Plans.Dtos;
using Ensa.Application.Contracts.Plans.Dtos.Navigations;
using Ensa.Domain.Lookups;
using Ensa.Domain.Plans;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Plans;

/// <summary>
/// Activity / document / revision catalogue application service.
/// <para>
/// <b>TENANCY.</b> <c>Activity</c> is a mixed host/tenant catalogue: rows with
/// <c>TenantId == null</c> are shared templates visible to every organisation, rows with a
/// tenant belong to that organisation alone. The visibility split is applied by the global
/// query filter in <c>EnsaDbContext</c>, so no method here adds a manual tenant predicate —
/// doing so would double-filter and hide the shared host entries.
/// </para>
/// <para>
/// Writes are a different matter: a tenant user must not be able to edit a shared host entry,
/// so the write paths check ownership explicitly before touching a row.
/// </para>
/// </summary>
public class ActivityAppService(
    IServiceProvider serviceProvider,
    IActivityRepository activityRepository,
    IReadOnlyRepository<ActivityGroup> activityGroupRepository,
    IReadOnlyRepository<Period> periodRepository)
    : EnsaAppService(serviceProvider), IActivityAppService
{
    /// <summary>Maximum number of records returned by a drop-down lookup.</summary>
    private const int LookupMaxRecord = 50;

    /// <inheritdoc />
    public async Task<ActivityDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Activity.Default);

        var activity = await activityRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Activity), id);

        return ObjectMapper.Map<Activity, ActivityDto>(activity);
    }

    /// <inheritdoc />
    public async Task<ActivityNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Activity.Default);

        // One repository call returns the activity, its group, its period, its parent and
        // its children — the children are not fetched one at a time.
        var navigation = await activityRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(Activity), id);

        return new ActivityNavigationDto
        {
            Activity = ObjectMapper.Map<Activity, ActivityDto>(navigation.Activity),
            ActivityGroup = navigation.ActivityGroup is null
                ? null
                : new LookupDto
                {
                    Id = navigation.ActivityGroup.Id,
                    DisplayName = navigation.ActivityGroup.GroupName,
                    IsActive = navigation.ActivityGroup.IsActive
                },
            Period = navigation.Period is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Period.Id,
                    DisplayName = navigation.Period.PeriodName
                },
            ParentActivity = navigation.ParentActivity is null
                ? null
                : new LookupDto
                {
                    Id = navigation.ParentActivity.Id,
                    DisplayName = navigation.ParentActivity.ActivityName,
                    Code = navigation.ParentActivity.ActivityCode,
                    IsActive = navigation.ParentActivity.IsActive
                },
            ChildActivities =
            [
                .. navigation.ChildActivities
                    .OrderBy(a => a.OrderNo ?? int.MaxValue)
                    .ThenBy(a => a.ActivityName, StringComparer.CurrentCulture)
                    .Select(a => new LookupDto
                    {
                        Id = a.Id,
                        DisplayName = a.ActivityName,
                        Code = a.ActivityCode,
                        IsActive = a.IsActive
                    })
            ]
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<ActivityListDto>> GetListAsync(
        GetActivityListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Activity.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "OrderNo ASC, ActivityName ASC");

        var total = await activityRepository.GetCountAsync(predicate, cancellationToken);

        var records = await activityRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Activity>, List<ActivityListDto>>(records);

        return new PagedResultDto<ActivityListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Activity.Default);

        var search = filter?.Trim();

        var records = await activityRepository.GetPagedListAsync(
            skipCount: 0,
            maxResultCount: LookupMaxRecord,
            sorting: "ActivityName ASC",
            predicate: string.IsNullOrEmpty(search)
                ? a => a.IsActive
                : a => a.IsActive && a.ActivityName.Contains(search),
            cancellationToken);

        var items = records
            .Select(a => new LookupDto
            {
                Id = a.Id,
                DisplayName = a.ActivityName,
                Code = a.ActivityCode,
                IsActive = a.IsActive
            })
            .ToList();

        return new ListResultDto<LookupDto>(items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<ActivityListDto>> GetDefaultsAsync(
        int? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Activity.Default);

        // The repository decides how the shared host entries and the organisation's own
        // entries are combined; the global query filter still applies on top.
        var records = await activityRepository.GetDefaultActivitiesAsync(
            tenantId ?? CurrentTenant.Id,
            cancellationToken);

        return new ListResultDto<ActivityListDto>(
            ObjectMapper.Map<List<Activity>, List<ActivityListDto>>(records));
    }

    /// <inheritdoc />
    public async Task<ActivityDto> CreateAsync(
        CreateActivityDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Activity.Create);

        await ValidateReferencesAsync(input, activityId: null, cancellationToken);

        var activity = ObjectMapper.Map<CreateActivityDto, Activity>(input);
        activity.IsActive = true;

        activity = await activityRepository.InsertAsync(activity, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Activity created. ActivityId={ActivityId}, Name={ActivityName}",
            activity.Id, activity.ActivityName);

        return ObjectMapper.Map<Activity, ActivityDto>(activity);
    }

    /// <inheritdoc />
    public async Task<ActivityDto> UpdateAsync(
        int id,
        UpdateActivityDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Activity.Update);

        var activity = await activityRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Activity), id);

        EnsureWritable(activity);

        await ValidateReferencesAsync(input, id, cancellationToken);

        ObjectMapper.Map(input, activity);

        activity = await activityRepository.UpdateAsync(activity, autoSave: true, cancellationToken);

        return ObjectMapper.Map<Activity, ActivityDto>(activity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Activity.Delete);

        var activity = await activityRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Activity), id);

        EnsureWritable(activity);

        var children = await activityRepository.GetChildActivitiesAsync(id, cancellationToken);

        // Deleting a parent would orphan its children's ParentActivityId, which no FK
        // enforces because the model carries no navigation properties.
        if (children.Count > 0)
        {
            throw new BusinessException(
                "An activity that still has child activities cannot be deleted.",
                "Ensa:Activity:HasChildActivities")
                .WithData("ActivityId", id)
                .WithData("ChildCount", children.Count);
        }

        await activityRepository.DeleteAsync(activity, autoSave: true, cancellationToken);

        Logger.LogInformation("Activity deleted. ActivityId={ActivityId}", id);
    }

    // -----------------------------------------------------------------

    /// <summary>
    /// Rejects a write against a shared host entry from inside a tenant context. Reads of
    /// those rows are fine — that is the point of a shared catalogue — but a single
    /// organisation must not be able to edit an entry every other organisation depends on.
    /// </summary>
    private void EnsureWritable(Activity activity)
    {
        if (activity.TenantId is null && CurrentTenant.Id is not null)
        {
            throw new BusinessException(
                "A shared catalogue entry can only be changed by a host administrator.",
                "Ensa:Activity:HostEntryIsReadOnlyForTenant")
                .WithData("ActivityId", activity.Id);
        }
    }

    /// <summary>Validates the group, period and parent references of an activity.</summary>
    private async Task ValidateReferencesAsync(
        CreateActivityDto input,
        int? activityId,
        CancellationToken cancellationToken)
    {
        if (input.ActivityGroupId is { } groupId
            && await activityGroupRepository.FindAsync(groupId, cancellationToken) is null)
        {
            throw new EntityNotFoundException(typeof(ActivityGroup), groupId);
        }

        if (input.PeriodId is { } periodId
            && await periodRepository.FindAsync(periodId, cancellationToken) is null)
        {
            throw new EntityNotFoundException(typeof(Period), periodId);
        }

        if (input.ParentActivityId is not { } parentId)
        {
            return;
        }

        if (activityId is { } id && parentId == id)
        {
            throw new BusinessException(
                "An activity cannot be its own parent.",
                "Ensa:Activity:SelfParentNotAllowed")
                .WithData("ActivityId", id);
        }

        _ = await activityRepository.FindAsync(parentId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Activity), parentId);
    }

    private static Expression<Func<Activity, bool>>? BuildFilter(GetActivityListInput input)
    {
        // No tenant predicate is built here on purpose: the global query filter already
        // returns the organisation's own rows plus the shared host rows.
        Expression<Func<Activity, bool>> predicate = a => true;
        var applied = false;

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var search = input.Filter.Trim();
            predicate = Combine(predicate, a =>
                a.ActivityName.Contains(search)
                || (a.ActivityCode != null && a.ActivityCode.Contains(search)));
            applied = true;
        }

        if (input.ActivityGroupId is { } groupId)
        {
            predicate = Combine(predicate, a => a.ActivityGroupId == groupId);
            applied = true;
        }

        if (input.ActivityType is { } activityType)
        {
            predicate = Combine(predicate, a => a.ActivityType == activityType);
            applied = true;
        }

        if (input.ParentActivityId is { } parentId)
        {
            predicate = Combine(predicate, a => a.ParentActivityId == parentId);
            applied = true;
        }

        if (input.PeriodId is { } periodId)
        {
            predicate = Combine(predicate, a => a.PeriodId == periodId);
            applied = true;
        }

        if (input.DefaultActivity is { } isDefault)
        {
            predicate = Combine(predicate, a => a.DefaultActivity == isDefault);
            applied = true;
        }

        if (input.IsActive is { } isActive)
        {
            predicate = Combine(predicate, a => a.IsActive == isActive);
            applied = true;
        }

        return applied ? predicate : null;
    }

    private static Expression<Func<Activity, bool>> Combine(
        Expression<Func<Activity, bool>> left,
        Expression<Func<Activity, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(Activity), "a");

        var body = Expression.AndAlso(
            new ParameterRebinder(left.Parameters[0], parameter).Visit(left.Body)!,
            new ParameterRebinder(right.Parameters[0], parameter).Visit(right.Body)!);

        return Expression.Lambda<Func<Activity, bool>>(body, parameter);
    }

    /// <summary>Rewrites two separate lambdas onto a single shared parameter.</summary>
    private sealed class ParameterRebinder(ParameterExpression previous, ParameterExpression replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == previous ? replacement : base.VisitParameter(node);
    }
}
