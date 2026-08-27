using Ensa.Domain.Common;
using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Trainings;
using Ensa.Application.Contracts.Trainings.Dtos;
using Ensa.Application.Contracts.Trainings.Dtos.Navigations;
using Ensa.Domain.Companies;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Ensa.Domain.Trainings;
using Microsoft.Extensions.Logging;
using Ensa.Domain.Membership;

namespace Ensa.Application.Trainings;

/// <summary>
/// Annual training plan application service — header plus lines, plus the per-line approval
/// workflow.
/// <para>
/// The approval workflow follows the same state machine the work plan module uses
/// (<c>Draft → ForApprovalSent → Approved | Rejected</c>, a rejected line may be
/// resubmitted). Unlike <c>WorkPlanLine</c>, <c>TrainingPlanLine</c> has no domain service
/// that owns those transitions, so they are validated here; the rule set is kept identical
/// on purpose so that the two plan modules never drift apart.
/// </para>
/// </summary>
public class TrainingPlanAppService(
    IServiceProvider serviceProvider,
    ITrainingPlanRepository planRepository,
    IRepository<TrainingPlanLine> lineRepository,
    IReadOnlyRepository<Training> trainingRepository,
    IReadOnlyRepository<Company> companyRepository,
    IPlanApprovalManager approvalManager,
    ITrainingPlanningManager planningManager,
    IUserRepository userRepository)
    : EnsaAppService(serviceProvider), ITrainingPlanAppService
{
    /// <inheritdoc />
    public async Task<TrainingPlanDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Default);

        var plan = await planRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(TrainingPlan), id);

        return ObjectMapper.Map<TrainingPlan, TrainingPlanDto>(plan);
    }

    /// <inheritdoc />
    public async Task<TrainingPlanNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Default);

        // One repository call returns the plan, the workplace, the staff and every line with
        // its training name and document name already joined — no per-line lookup here.
        var navigation = await planRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(TrainingPlan), id);

        // Every name this view shows, in one query. Names live on the profile now, so a
        // User in hand is no longer enough to render one.
        var names = await userRepository.GetDisplaysAsync(
            new[] { navigation.Specialist?.Id, navigation.Physician?.Id, navigation.Approver?.Id }
                .Concat(navigation.Lines.Select(l => l.InstructorUser?.Id))
                .Where(x => x.HasValue)
                .Select(x => x!.Value),
            cancellationToken);


        return new TrainingPlanNavigationDto
        {
            TrainingPlan = ObjectMapper.Map<TrainingPlan, TrainingPlanDto>(navigation.TrainingPlan),
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
                .. navigation.Lines.Select(l => new TrainingPlanLineNavigationDto
                {
                    Line = ObjectMapper.Map<TrainingPlanLine, TrainingPlanLineDto>(l.TrainingPlanLine),
                    TrainingName = l.TrainingName,
                    InstructorUserFullName = FullName(names, l.InstructorUser),
                    DocumentName = l.DocumentName
                })
            ]
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<TrainingPlanListDto>> GetListAsync(
        GetTrainingPlanListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "StartDate DESC");

        var total = await planRepository.GetCountAsync(predicate, cancellationToken);

        var records = await planRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<TrainingPlan>, List<TrainingPlanListDto>>(records);

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
                l => planIds.Contains(l.TrainingPlanId),
                cancellationToken);

            var companyNames = companies.ToDictionary(c => c.Id, c => c.CompanyName);
            var lineCounts = lines
                .GroupBy(l => l.TrainingPlanId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var item in items)
            {
                if (companyNames.TryGetValue(item.CompanyId, out var companyName))
                {
                    item.CompanyName = companyName;
                }

                item.LineCount = lineCounts.TryGetValue(item.Id, out var count) ? count : 0;
            }
        }

        return new PagedResultDto<TrainingPlanListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<TrainingPlanDto?> GetActivePlanAsync(
        int companyId,
        int year,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Default);
        ValidateCalendarYear(year);

        var plan = await planRepository.GetActivePlanAsync(companyId, year, cancellationToken);

        return plan is null ? null : ObjectMapper.Map<TrainingPlan, TrainingPlanDto>(plan);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<TrainingPlanLineDto>> GetIncompleteLinesAsync(
        int planId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Default);

        _ = await planRepository.FindAsync(planId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(TrainingPlan), planId);

        var lines = await planRepository.GetIncompleteLinesAsync(planId, cancellationToken);

        return new ListResultDto<TrainingPlanLineDto>(
            ObjectMapper.Map<List<TrainingPlanLine>, List<TrainingPlanLineDto>>(lines));
    }

    /// <inheritdoc />
    public async Task<TrainingPlanDto> CreateAsync(
        CreateTrainingPlanDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Create);

        var year = input.StartDate.Year;

        // The single-active-plan rule is an invariant of the plan, so the manager owns it.
        await planningManager.ValidateSingleActivePlanAsync(input.CompanyId, year, null, cancellationToken);

        var plan = ObjectMapper.Map<CreateTrainingPlanDto, TrainingPlan>(input);
        plan.IsActive = true;

        plan = await planRepository.InsertAsync(plan, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Training plan created. PlanId={PlanId}, CompanyId={CompanyId}", plan.Id, plan.CompanyId);

        return ObjectMapper.Map<TrainingPlan, TrainingPlanDto>(plan);
    }

    /// <inheritdoc />
    public async Task<TrainingPlanDto> UpdateAsync(
        int id,
        UpdateTrainingPlanDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Update);

        var plan = await planRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(TrainingPlan), id);

        if (input.IsActive)
        {
            await planningManager.ValidateSingleActivePlanAsync(
                input.CompanyId, input.StartDate.Year, id, cancellationToken);
        }

        ObjectMapper.Map(input, plan);

        plan = await planRepository.UpdateAsync(plan, autoSave: true, cancellationToken);

        return ObjectMapper.Map<TrainingPlan, TrainingPlanDto>(plan);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Delete);

        var plan = await planRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(TrainingPlan), id);

        var lines = await planRepository.GetLinesAsync(id, cancellationToken);

        // An approved line is a record of a completed statutory obligation; the plan that
        // carries it is not deleted.
        if (lines.Exists(l => l.ApprovalStatus == ApprovalStatus.Approved))
        {
            throw new BusinessException(
                "A training plan that contains approved lines cannot be deleted.",
                "Ensa:TrainingPlan:ApprovedLinesCannotBeDeleted")
                .WithData("PlanId", id);
        }

        if (lines.Count > 0)
        {
            await lineRepository.DeleteManyAsync(lines, autoSave: false, cancellationToken);
        }

        await planRepository.DeleteAsync(plan, autoSave: true, cancellationToken);

        Logger.LogInformation("Training plan deleted. PlanId={PlanId}", id);
    }

    // ------------------------------------------------------------------ Lines

    /// <inheritdoc />
    public async Task<PagedResultDto<TrainingPlanLineListDto>> GetLineListAsync(
        GetTrainingPlanLineListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Default);

        var predicate = BuildLineFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "Year DESC, Month DESC");

        var total = await lineRepository.GetCountAsync(predicate, cancellationToken);

        var records = await lineRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = records.ConvertAll(line => new TrainingPlanLineListDto
        {
            Id = line.Id,
            TrainingPlanId = line.TrainingPlanId,
            Year = line.Year,
            Month = line.Month,
            DurationMinutes = line.DurationMinutes,
            Status = line.Status,
            ApprovalStatus = line.ApprovalStatus,
            PerformedDate = line.PerformedDate,
            InstructorFullName = line.InstructorFullName
        });

        if (items.Count > 0)
        {
            // Two batched queries for the whole page - companies and trainings - so the cost is
            // independent of how many rows the page holds.
            var companyIds = records
                .Where(l => l.CompanyId is > 0)
                .Select(l => l.CompanyId!.Value)
                .Distinct()
                .ToList();

            var trainingIds = records.Select(l => l.TrainingId).Distinct().ToList();

            var companyNames = companyIds.Count == 0
                ? []
                : (await companyRepository.GetListAsync(c => companyIds.Contains(c.Id), cancellationToken))
                    .ToDictionary(c => c.Id, c => c.CompanyName);

            var trainingNames = (await trainingRepository
                    .GetListAsync(t => trainingIds.Contains(t.Id), cancellationToken))
                .ToDictionary(t => t.Id, t => t.TrainingName);

            for (var i = 0; i < items.Count; i++)
            {
                var line = records[i];

                if (line.CompanyId is { } companyId && companyNames.TryGetValue(companyId, out var companyName))
                {
                    items[i].CompanyName = companyName;
                }

                if (trainingNames.TryGetValue(line.TrainingId, out var trainingName))
                {
                    items[i].TrainingName = trainingName;
                }
            }
        }

        return new PagedResultDto<TrainingPlanLineListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<TrainingPlanLineDto>> GetLinesAsync(
        int planId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Default);

        _ = await planRepository.FindAsync(planId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(TrainingPlan), planId);

        var lines = await planRepository.GetLinesAsync(planId, cancellationToken);

        return new ListResultDto<TrainingPlanLineDto>(
            ObjectMapper.Map<List<TrainingPlanLine>, List<TrainingPlanLineDto>>(lines));
    }

    /// <inheritdoc />
    public async Task<TrainingPlanLineDto> AddLineAsync(
        int planId,
        CreateTrainingPlanLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Create);

        var plan = await planRepository.FindAsync(planId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(TrainingPlan), planId);

        _ = await trainingRepository.FindAsync(input.TrainingId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Training), input.TrainingId);

        var line = ObjectMapper.Map<CreateTrainingPlanLineDto, TrainingPlanLine>(input);
        line.TrainingPlanId = planId;
        line.CompanyId = plan.CompanyId;
        line.Year ??= plan.StartDate.Year;
        line.IsActive = true;
        line.ApprovalStatus = ApprovalStatus.Draft;
        line.IbysStatus = IbysSubmissionStatus.NotSent;
        line.RejectionReason = null;

        line = await lineRepository.InsertAsync(line, autoSave: true, cancellationToken);

        return ObjectMapper.Map<TrainingPlanLine, TrainingPlanLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<TrainingPlanLineDto> UpdateLineAsync(
        int planId,
        int lineId,
        UpdateTrainingPlanLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Update);

        var line = await FindOwnedLineAsync(planId, lineId, cancellationToken);

        // An approved line records a delivered training; editing it would rewrite history.
        if (line.ApprovalStatus == ApprovalStatus.Approved)
        {
            throw new BusinessException(
                "An approved plan line can no longer be edited.",
                "Ensa:TrainingPlan:ApprovedLineIsReadOnly")
                .WithData("LineId", lineId);
        }

        ObjectMapper.Map(input, line);
        line.TrainingPlanId = planId;

        line = await lineRepository.UpdateAsync(line, autoSave: true, cancellationToken);

        return ObjectMapper.Map<TrainingPlanLine, TrainingPlanLineDto>(line);
    }

    /// <inheritdoc />
    public async Task RemoveLineAsync(int planId, int lineId, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Delete);

        var line = await FindOwnedLineAsync(planId, lineId, cancellationToken);

        if (line.ApprovalStatus == ApprovalStatus.Approved)
        {
            throw new BusinessException(
                "An approved plan line can no longer be removed.",
                "Ensa:TrainingPlan:ApprovedLineIsReadOnly")
                .WithData("LineId", lineId);
        }

        await lineRepository.DeleteAsync(line, autoSave: true, cancellationToken);
    }

    // --------------------------------------------------------- Approval flow

    /// <inheritdoc />
    public async Task<TrainingPlanLineDto> SubmitLineForApprovalAsync(
        int planId,
        int lineId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Update);

        var line = await FindOwnedLineAsync(planId, lineId, cancellationToken);
        var userId = GetRequiredUserId();

        // Shared with the work plan module so the two workflows cannot drift apart. It writes
        // the fields in memory only; the update below is the single write.
        approvalManager.ApplyTransition(
            line,
            ApprovalStatus.SubmittedForApproval,
            userId,
            Clock.Now,
            "Ensa:TrainingPlan:InvalidApprovalTransition");

        line = await lineRepository.UpdateAsync(line, autoSave: true, cancellationToken);

        Logger.LogInformation("Training plan line submitted for approval. LineId={LineId}", lineId);

        return ObjectMapper.Map<TrainingPlanLine, TrainingPlanLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<TrainingPlanLineDto> ApproveLineAsync(
        int planId,
        int lineId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Approve);

        var line = await FindOwnedLineAsync(planId, lineId, cancellationToken);
        var userId = GetRequiredUserId();

        approvalManager.ApplyTransition(
            line,
            ApprovalStatus.Approved,
            userId,
            Clock.Now,
            "Ensa:TrainingPlan:InvalidApprovalTransition");

        line = await lineRepository.UpdateAsync(line, autoSave: true, cancellationToken);

        Logger.LogInformation("Training plan line approved. LineId={LineId}, UserId={UserId}", lineId, userId);

        return ObjectMapper.Map<TrainingPlanLine, TrainingPlanLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<TrainingPlanLineDto> RejectLineAsync(
        int planId,
        int lineId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        await CheckPermissionAsync(EnsaPermissions.TrainingPlan.Approve);

        var line = await FindOwnedLineAsync(planId, lineId, cancellationToken);
        var userId = GetRequiredUserId();

        approvalManager.ApplyTransition(
            line,
            ApprovalStatus.Rejected,
            userId,
            Clock.Now,
            "Ensa:TrainingPlan:InvalidApprovalTransition",
            reason);

        line = await lineRepository.UpdateAsync(line, autoSave: true, cancellationToken);

        Logger.LogInformation("Training plan line rejected. LineId={LineId}, UserId={UserId}", lineId, userId);

        return ObjectMapper.Map<TrainingPlanLine, TrainingPlanLineDto>(line);
    }

    // -----------------------------------------------------------------

    private async Task<TrainingPlanLine> FindOwnedLineAsync(
        int planId,
        int lineId,
        CancellationToken cancellationToken)
    {
        var line = await lineRepository.FindAsync(lineId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(TrainingPlanLine), lineId);

        // A line belonging to another plan is reported as missing rather than as forbidden,
        // so the endpoint cannot be used to probe for line ids.
        if (line.TrainingPlanId != planId)
        {
            throw new EntityNotFoundException(typeof(TrainingPlanLine), lineId);
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

    /// <summary>
    /// Builds the cross-plan line filter. The free-text term matches the instructor name and the
    /// line description only — the caller filters by company or training through their ids, which
    /// keeps this a single indexed query instead of a join against two other tables.
    /// </summary>
    private static Expression<Func<TrainingPlanLine, bool>>? BuildLineFilter(GetTrainingPlanLineListInput input)
    {
        Expression<Func<TrainingPlanLine, bool>> predicate = l => true;
        var applied = false;

        void Add(Expression<Func<TrainingPlanLine, bool>> clause)
        {
            predicate = applied ? Combine(predicate, clause) : clause;
            applied = true;
        }

        if (input.TrainingPlanId is { } planId)
        {
            Add(l => l.TrainingPlanId == planId);
        }

        if (input.CompanyId is { } companyId)
        {
            Add(l => l.CompanyId == companyId);
        }

        if (input.TrainingId is { } trainingId)
        {
            Add(l => l.TrainingId == trainingId);
        }

        if (input.Year is { } year)
        {
            Add(l => l.Year == year);
        }

        if (input.Month is { } month)
        {
            Add(l => l.Month == month);
        }

        if (input.Status is { } status)
        {
            Add(l => l.Status == status);
        }

        if (input.ApprovalStatus is { } approvalStatus)
        {
            Add(l => l.ApprovalStatus == approvalStatus);
        }

        if (input.IsActive is { } isActive)
        {
            Add(l => l.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var term = input.Filter.Trim();
            Add(l => (l.InstructorFullName != null && l.InstructorFullName.Contains(term))
                     || (l.Description != null && l.Description.Contains(term)));
        }

        return applied ? predicate : null;
    }

    private static Expression<Func<TrainingPlan, bool>>? BuildFilter(GetTrainingPlanListInput input)
    {
        Expression<Func<TrainingPlan, bool>> predicate = p => true;
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

        if (input.Transferred is { } transferred)
        {
            predicate = Combine(predicate, p => p.Transferred == transferred);
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

    /// <summary>ANDs two predicates over the same entity onto one shared parameter.</summary>
    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        var body = Expression.AndAlso(
            new ParameterRebinder(left.Parameters[0], parameter).Visit(left.Body)!,
            new ParameterRebinder(right.Parameters[0], parameter).Visit(right.Body)!);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    /// <summary>Rewrites two separate lambdas onto a single shared parameter.</summary>
    private sealed class ParameterRebinder(ParameterExpression previous, ParameterExpression replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == previous ? replacement : base.VisitParameter(node);
    }
}
