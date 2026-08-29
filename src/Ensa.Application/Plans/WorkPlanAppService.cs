using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Plans;
using Ensa.Application.Contracts.Plans.Dtos;
using Ensa.Application.Contracts.Plans.Dtos.Navigations;
using Ensa.Domain.Companies;
using Ensa.Domain.Plans;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Ensa.Domain.Membership;

namespace Ensa.Application.Plans;

/// <summary>
/// Annual occupational health and safety work plan application service — header plus lines.
/// <para>
/// Three rules belong to <see cref="IWorkPlanManager"/> and are called, never reproduced
/// here: the "one active plan per workplace and year" invariant
/// (<c>ValidateSingleActivePlanAsync</c>), the per-line approval state machine
/// (<c>ApplyApprovalTransition</c>) and the generation of default lines from the activity
/// catalogue (<c>GenerateDefaultLines</c>).
/// </para>
/// <para>
/// All three are pure: the manager validates, mutates in memory and returns, but performs
/// no persistence of its own. This service therefore saves the entities itself — exactly
/// once per call.
/// </para>
/// </summary>
public class WorkPlanAppService(
    IServiceProvider serviceProvider,
    IWorkPlanRepository planRepository,
    IWorkPlanManager workPlanManager,
    IActivityRepository activityRepository,
    IRepository<WorkPlanLine> lineRepository,
    IReadOnlyRepository<Company> companyRepository,
    IUserRepository userRepository)
    : EnsaAppService(serviceProvider), IWorkPlanAppService
{
    /// <inheritdoc />
    public async Task<WorkPlanDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.WorkPlan.Default);

        var plan = await planRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(WorkPlan), id);

        return ObjectMapper.Map<WorkPlan, WorkPlanDto>(plan);
    }

    /// <inheritdoc />
    public async Task<WorkPlanNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.WorkPlan.Default);

        // One repository call returns the plan, the workplace, the staff and every line with
        // its activity name and document name already joined — no per-line lookup here.
        var navigation = await planRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(WorkPlan), id);

        // Every name this view shows, in one query. Names live on the profile now, so a
        // User in hand is no longer enough to render one.
        var names = await userRepository.GetDisplaysAsync(
            new[] { navigation.Specialist?.Id, navigation.Physician?.Id, navigation.Approver?.Id }
                .Concat(navigation.Lines.Select(l => l.InstructorUser?.Id))
                .Where(x => x.HasValue)
                .Select(x => x!.Value),
            cancellationToken);


        return new WorkPlanNavigationDto
        {
            WorkPlan = ObjectMapper.Map<WorkPlan, WorkPlanDto>(navigation.WorkPlan),
            Company = navigation.Company is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Company.Id,
                    DisplayName = navigation.Company.CompanyName,
                    Code = navigation.Company.SsiNumber,
                    IsActive = navigation.Company.IsActive
                },
            SpecialistFullName = FullName(names, navigation.Specialist),
            PhysicianFullName = FullName(names, navigation.Physician),
            ApproverFullName = FullName(names, navigation.Approver),
            Lines =
            [
                .. navigation.Lines.Select(l => new WorkPlanLineNavigationDto
                {
                    Line = ObjectMapper.Map<WorkPlanLine, WorkPlanLineDto>(l.WorkPlanLine),
                    ActivityName = l.ActivityName,
                    InstructorUserFullName = FullName(names, l.InstructorUser),
                    DocumentName = l.DocumentName
                })
            ]
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<WorkPlanListDto>> GetListAsync(
        GetWorkPlanListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.WorkPlan.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "StartDate DESC");

        var total = await planRepository.GetCountAsync(predicate, cancellationToken);

        var records = await planRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<WorkPlan>, List<WorkPlanListDto>>(records);

        if (items.Count > 0)
        {
            // Two batched queries for the whole page: workplace names, and line counts
            // grouped in memory. Neither scales with the number of rows.
            var planIds = records.Select(p => p.Id).ToList();
            var companyIds = records.Select(p => p.CompanyId).Distinct().ToList();

            var companies = await companyRepository.GetListAsync(
                c => companyIds.Contains(c.Id),
                cancellationToken);

            var lines = await lineRepository.GetListAsync(
                l => planIds.Contains(l.WorkPlanId),
                cancellationToken);

            var companyNames = companies.ToDictionary(c => c.Id, c => c.CompanyName);
            var lineCounts = lines.GroupBy(l => l.WorkPlanId).ToDictionary(g => g.Key, g => g.Count());

            foreach (var item in items)
            {
                if (companyNames.TryGetValue(item.CompanyId, out var companyName))
                {
                    item.CompanyName = companyName;
                }

                item.LineCount = lineCounts.TryGetValue(item.Id, out var count) ? count : 0;
            }
        }

        return new PagedResultDto<WorkPlanListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<WorkPlanDto?> GetActivePlanAsync(
        int companyId,
        int year,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.WorkPlan.Default);
        ValidateCalendarYear(year);

        var plan = await planRepository.GetActivePlanAsync(companyId, year, cancellationToken);

        return plan is null ? null : ObjectMapper.Map<WorkPlan, WorkPlanDto>(plan);
    }

    /// <inheritdoc />
    public async Task<WorkPlanDto> CreateAsync(
        CreateWorkPlanDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.WorkPlan.Create);

        // The single-active-plan invariant belongs to the manager; it only validates and
        // saves nothing.
        await workPlanManager.ValidateSingleActivePlanAsync(
            input.CompanyId,
            input.StartDate.Year,
            exceptPlanId: null,
            cancellationToken);

        var plan = ObjectMapper.Map<CreateWorkPlanDto, WorkPlan>(input);
        plan.IsActive = true;

        plan = await planRepository.InsertAsync(plan, autoSave: true, cancellationToken);

        Logger.LogInformation("Work plan created. PlanId={PlanId}, CompanyId={CompanyId}", plan.Id, plan.CompanyId);

        return ObjectMapper.Map<WorkPlan, WorkPlanDto>(plan);
    }

    /// <inheritdoc />
    public async Task<WorkPlanDto> UpdateAsync(
        int id,
        UpdateWorkPlanDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.WorkPlan.Update);

        var plan = await planRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(WorkPlan), id);

        if (input.IsActive)
        {
            await workPlanManager.ValidateSingleActivePlanAsync(
                input.CompanyId,
                input.StartDate.Year,
                exceptPlanId: id,
                cancellationToken);
        }

        ObjectMapper.Map(input, plan);

        plan = await planRepository.UpdateAsync(plan, autoSave: true, cancellationToken);

        return ObjectMapper.Map<WorkPlan, WorkPlanDto>(plan);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.WorkPlan.Delete);

        var plan = await planRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(WorkPlan), id);

        var lines = await planRepository.GetLinesAsync(id, cancellationToken);

        // An approved line records a completed statutory obligation; the plan carrying it is
        // not deleted.
        if (lines.Exists(l => l.ApprovalStatus == ApprovalStatus.Approved))
        {
            throw new BusinessException(
                "A work plan that contains approved lines cannot be deleted.",
                "Ensa:WorkPlan:ApprovedLinesCannotBeDeleted")
                .WithData("PlanId", id);
        }

        if (lines.Count > 0)
        {
            await lineRepository.DeleteManyAsync(lines, autoSave: false, cancellationToken);
        }

        await planRepository.DeleteAsync(plan, autoSave: true, cancellationToken);

        Logger.LogInformation("Work plan deleted. PlanId={PlanId}", id);
    }

    // ------------------------------------------------------------------ Lines

    /// <inheritdoc />
    public async Task<ListResultDto<WorkPlanLineDto>> GetLinesAsync(
        int planId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.WorkPlan.Default);

        _ = await planRepository.FindAsync(planId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(WorkPlan), planId);

        var lines = await planRepository.GetLinesAsync(planId, cancellationToken);

        return new ListResultDto<WorkPlanLineDto>(
            ObjectMapper.Map<List<WorkPlanLine>, List<WorkPlanLineDto>>(lines));
    }

    /// <inheritdoc />
    public async Task<WorkPlanLineDto> AddLineAsync(
        int planId,
        CreateWorkPlanLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.WorkPlan.Create);

        var plan = await planRepository.FindAsync(planId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(WorkPlan), planId);

        _ = await activityRepository.FindAsync(input.ActivityId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Activity), input.ActivityId);

        var line = ObjectMapper.Map<CreateWorkPlanLineDto, WorkPlanLine>(input);
        line.WorkPlanId = planId;
        line.CompanyId = plan.CompanyId;
        line.IsActive = true;
        line.ApprovalStatus = ApprovalStatus.Draft;

        if (line.Year == 0)
        {
            line.Year = plan.StartDate.Year;
        }

        line = await lineRepository.InsertAsync(line, autoSave: true, cancellationToken);

        return ObjectMapper.Map<WorkPlanLine, WorkPlanLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<WorkPlanLineDto> UpdateLineAsync(
        int planId,
        int lineId,
        UpdateWorkPlanLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.WorkPlan.Update);

        var line = await FindOwnedLineAsync(planId, lineId, cancellationToken);

        if (line.ApprovalStatus == ApprovalStatus.Approved)
        {
            throw new BusinessException(
                "An approved plan line can no longer be edited.",
                "Ensa:WorkPlan:ApprovedLineIsReadOnly")
                .WithData("LineId", lineId);
        }

        ObjectMapper.Map(input, line);
        line.WorkPlanId = planId;

        line = await lineRepository.UpdateAsync(line, autoSave: true, cancellationToken);

        return ObjectMapper.Map<WorkPlanLine, WorkPlanLineDto>(line);
    }

    /// <inheritdoc />
    public async Task RemoveLineAsync(int planId, int lineId, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.WorkPlan.Delete);

        var line = await FindOwnedLineAsync(planId, lineId, cancellationToken);

        if (line.ApprovalStatus == ApprovalStatus.Approved)
        {
            throw new BusinessException(
                "An approved plan line can no longer be removed.",
                "Ensa:WorkPlan:ApprovedLineIsReadOnly")
                .WithData("LineId", lineId);
        }

        await lineRepository.DeleteAsync(line, autoSave: true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<WorkPlanLineDto>> GenerateDefaultLinesAsync(
        int planId,
        int year,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.WorkPlan.Create);
        ValidateCalendarYear(year);

        var plan = await planRepository.FindAsync(planId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(WorkPlan), planId);

        var existing = await planRepository.GetLinesAsync(planId, cancellationToken);

        // Generation is a one-off scaffolding step; running it twice would duplicate every
        // line and quietly double the workload the plan reports.
        if (existing.Count > 0)
        {
            throw new BusinessException(
                "Default lines can only be generated for a plan that has no lines yet.",
                "Ensa:WorkPlan:DefaultLinesAlreadyGenerated")
                .WithData("PlanId", planId);
        }

        var activities = await activityRepository.GetDefaultActivitiesAsync(plan.TenantId, cancellationToken);

        // One query for every period mapping of the whole default set — not one per activity,
        // which is what a per-activity GetPeriodsAsync loop would have cost.
        var periods = await activityRepository.GetPeriodsAsync(
            activities.Select(a => a.Id),
            cancellationToken);

        // The spread across the year (count, month offset, interval) belongs to the manager.
        var lines = workPlanManager.GenerateDefaultLines(
            planId,
            plan.CompanyId,
            activities,
            periods,
            year);

        // GenerateDefaultLines only builds the objects; nothing is saved by the manager, so
        // the lines are inserted here.
        if (lines.Count > 0)
        {
            await lineRepository.InsertManyAsync(lines, autoSave: true, cancellationToken);
        }

        Logger.LogInformation(
            "Default work plan lines generated. PlanId={PlanId}, Year={Year}, Count={Count}",
            planId, year, lines.Count);

        return new ListResultDto<WorkPlanLineDto>(
            ObjectMapper.Map<List<WorkPlanLine>, List<WorkPlanLineDto>>(lines));
    }

    /// <inheritdoc />
    public async Task<WorkPlanCompletionDto> GetCompletionRateAsync(
        int planId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.WorkPlan.Default);

        _ = await planRepository.FindAsync(planId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(WorkPlan), planId);

        // Computed by the repository in SQL rather than by loading every line.
        var rate = await planRepository.GetCompletionRateAsync(planId, cancellationToken);

        return new WorkPlanCompletionDto
        {
            WorkPlanId = planId,
            CompletionRate = rate,
            CompletionPercentage = Math.Round(rate * 100, 2, MidpointRounding.AwayFromZero)
        };
    }

    // --------------------------------------------------------- Approval flow

    /// <inheritdoc />
    public Task<WorkPlanLineDto> SubmitLineForApprovalAsync(
        int planId,
        int lineId,
        CancellationToken cancellationToken = default)
        => TransitionLineAsync(
            planId,
            lineId,
            ApprovalStatus.SubmittedForApproval,
            EnsaPermissions.WorkPlan.Update,
            reason: null,
            cancellationToken);

    /// <inheritdoc />
    public Task<WorkPlanLineDto> ApproveLineAsync(
        int planId,
        int lineId,
        CancellationToken cancellationToken = default)
        => TransitionLineAsync(
            planId,
            lineId,
            ApprovalStatus.Approved,
            EnsaPermissions.WorkPlan.Approve,
            reason: null,
            cancellationToken);

    /// <inheritdoc />
    public Task<WorkPlanLineDto> RejectLineAsync(
        int planId,
        int lineId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return TransitionLineAsync(
            planId,
            lineId,
            ApprovalStatus.Rejected,
            EnsaPermissions.WorkPlan.Approve,
            reason.Trim(),
            cancellationToken);
    }

    // -----------------------------------------------------------------

    /// <summary>
    /// Runs one approval transition through <see cref="IWorkPlanManager.ApplyApprovalTransition"/>
    /// and persists the result.
    /// </summary>
    private async Task<WorkPlanLineDto> TransitionLineAsync(
        int planId,
        int lineId,
        ApprovalStatus target,
        string permission,
        string? reason,
        CancellationToken cancellationToken)
    {
        await CheckPermissionAsync(permission);

        var line = await FindOwnedLineAsync(planId, lineId, cancellationToken);
        var userId = GetRequiredUserId();

        // The manager validates the edge and fills in the workflow fields - the rejection reason
        // included - in memory; it does not save, so the update below is the only write.
        workPlanManager.ApplyApprovalTransition(line, target, userId, Clock.Now, reason);

        line = await lineRepository.UpdateAsync(line, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Work plan line approval transition. LineId={LineId}, Status={Status}, UserId={UserId}",
            lineId, target, userId);

        return ObjectMapper.Map<WorkPlanLine, WorkPlanLineDto>(line);
    }

    private async Task<WorkPlanLine> FindOwnedLineAsync(
        int planId,
        int lineId,
        CancellationToken cancellationToken)
    {
        var line = await lineRepository.FindAsync(lineId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(WorkPlanLine), lineId);

        // A line belonging to another plan is reported as missing rather than as forbidden,
        // so the endpoint cannot be used to probe for line ids.
        if (line.WorkPlanId != planId)
        {
            throw new EntityNotFoundException(typeof(WorkPlanLine), lineId);
        }

        return line;
    }

    /// <summary>
    /// The name to show for a user, looked up by id. The name lives on the profile now, so a
    /// User in hand is no longer enough to produce one — and the caller has already fetched
    /// every name it needs in a single query.
    /// </summary>
    private static string? FullName(
        IReadOnlyDictionary<int, UserDisplay> names,
        Ensa.Domain.Membership.User? user)
        => user is not null && names.TryGetValue(user.Id, out var display)
            ? display.DisplayName
            : null;

    private static Expression<Func<WorkPlan, bool>>? BuildFilter(GetWorkPlanListInput input)
    {
        Expression<Func<WorkPlan, bool>> predicate = p => true;
        var applied = false;

        if (input.CompanyId is { } companyId)
        {
            predicate = Combine(predicate, p => p.CompanyId == companyId);
            applied = true;
        }

        if (input.Year is { } year)
        {
            predicate = Combine(predicate, p => p.StartDate.Year == year);
            applied = true;
        }

        if (input.SpecialistUserId is { } specialistId)
        {
            predicate = Combine(predicate, p => p.SpecialistUserId == specialistId);
            applied = true;
        }

        if (input.PhysicianUserId is { } physicianId)
        {
            predicate = Combine(predicate, p => p.PhysicianUserId == physicianId);
            applied = true;
        }

        if (input.IsActive is { } isActive)
        {
            predicate = Combine(predicate, p => p.IsActive == isActive);
            applied = true;
        }

        if (input.IsTransferred is { } transferred)
        {
            predicate = Combine(predicate, p => p.IsTransferred == transferred);
            applied = true;
        }

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var search = input.Filter.Trim();
            predicate = Combine(predicate, p =>
                (p.DocumentNo != null && p.DocumentNo.Contains(search))
                || (p.RevisionNo != null && p.RevisionNo.Contains(search)));
            applied = true;
        }

        return applied ? predicate : null;
    }

    private static Expression<Func<WorkPlan, bool>> Combine(
        Expression<Func<WorkPlan, bool>> left,
        Expression<Func<WorkPlan, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(WorkPlan), "p");

        var body = Expression.AndAlso(
            new ParameterRebinder(left.Parameters[0], parameter).Visit(left.Body)!,
            new ParameterRebinder(right.Parameters[0], parameter).Visit(right.Body)!);

        return Expression.Lambda<Func<WorkPlan, bool>>(body, parameter);
    }

    /// <summary>Rewrites two separate lambdas onto a single shared parameter.</summary>
    private sealed class ParameterRebinder(ParameterExpression previous, ParameterExpression replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == previous ? replacement : base.VisitParameter(node);
    }
}
