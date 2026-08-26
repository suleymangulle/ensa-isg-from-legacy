using Ensa.Domain.Common;
using Ensa.Domain.Services;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Domain.Plans;

/// <summary>
/// Domain service that enforces the work plan business rules:
/// <list type="bullet">
/// <item>A company can have only one active plan per year.</item>
/// <item>Plan line approval workflow: <c>Draft → ForApprovalSent → Approved/Rejected</c>
/// (a rejected line can be resubmitted for approval).</item>
/// <item>Generating the lines of a new plan from the default activities.</item>
/// </list>
/// </summary>
public interface IWorkPlanManager : IDomainService
{
    /// <summary>
    /// Verifies that no other active work plan exists for a company and year. Throws
    /// <see cref="BusinessException"/> when one does and it is not <paramref name="exceptPlanId"/>.
    /// </summary>
    Task ValidateSingleActivePlanAsync(int companyId, int year, int? exceptPlanId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a plan line's approval status to <paramref name="targetStatus"/>, delegating to
    /// <see cref="IPlanApprovalManager"/> so work plans and training plans cannot drift apart.
    /// Throws <see cref="BusinessException"/> when the transition is not allowed; on a valid one
    /// the workflow fields are written in memory and the caller owns the save.
    /// </summary>
    void ApplyApprovalTransition(
        WorkPlanLine line,
        ApprovalStatus targetStatus,
        int userId,
        DateTime? date = null,
        string? rejectionReason = null);

    /// <summary>
    /// Generates plan lines spread across the year for a work plan, from the given default activities
    /// and period mappings. (Legacy: the generation logic built on the
    /// <c>Aktivite_T.DefaultAktivite</c>, <c>DefaultCount</c> and <c>DefaultStartMonthOffset</c>
    /// columns.)
    /// </summary>
    List<WorkPlanLine> GenerateDefaultLines(
        int workPlanId,
        int companyId,
        IEnumerable<Activity> activities,
        IEnumerable<ActivityPeriod> periods,
        int year);
}

/// <inheritdoc cref="IWorkPlanManager"/>
public class WorkPlanManager : DomainService, IWorkPlanManager
{
    private readonly IWorkPlanRepository _workPlanRepository;
    private readonly IClock _clock;
    private readonly IPlanApprovalManager _approvalManager;

    public WorkPlanManager(
        IWorkPlanRepository workPlanRepository,
        IClock clock,
        IPlanApprovalManager approvalManager)
    {
        _workPlanRepository = workPlanRepository;
        _clock = clock;
        _approvalManager = approvalManager;
    }

    public async Task ValidateSingleActivePlanAsync(
        int companyId,
        int year,
        int? exceptPlanId = null,
        CancellationToken cancellationToken = default)
    {
        var existingActivePlan = await _workPlanRepository.GetActivePlanAsync(companyId, year, cancellationToken);

        if (existingActivePlan is not null && existingActivePlan.Id != exceptPlanId)
        {
            throw new BusinessException(
                $"Company {companyId} already has an active work plan for {year}. " +
                "Deactivate the current plan before creating a new one.",
                "Ensa:WorkPlan:SingleActivePlanViolation");
        }
    }

    public void ApplyApprovalTransition(
        WorkPlanLine line,
        ApprovalStatus targetStatus,
        int userId,
        DateTime? date = null,
        string? rejectionReason = null)
        => _approvalManager.ApplyTransition(
            line,
            targetStatus,
            userId,
            date ?? _clock.Now,
            "Ensa:WorkPlan:InvalidApprovalTransition",
            rejectionReason);

    public List<WorkPlanLine> GenerateDefaultLines(
        int workPlanId,
        int companyId,
        IEnumerable<Activity> activities,
        IEnumerable<ActivityPeriod> periods,
        int year)
    {
        var periodMappings = periods.ToList();
        var lines = new List<WorkPlanLine>();

        foreach (var activity in activities.Where(a => a.DefaultActivity && a.IsActive))
        {
            // Use the activity-specific period mapping when there is one, otherwise the activity's
            // own PeriodId.
            var periodId = periodMappings
                .FirstOrDefault(p => p.ActivityId == activity.Id)?.PeriodId ?? activity.PeriodId;

            // How many times a year it is scheduled (at least 1, at most 12 — monthly) and the
            // starting month offset.
            var count = activity.DefaultCount <= 0 ? 1 : Math.Min(activity.DefaultCount, 12);
            var startMonthOffset = ((activity.DefaultStartMonthOffset % 12) + 12) % 12;
            var startMonth = startMonthOffset + 1;

            // More than one occurrence is spread evenly across the year (e.g. 4 occurrences → once
            // every 3 months).
            var monthInterval = count <= 1 ? 12 : 12 / count;

            for (var i = 0; i < count; i++)
            {
                var month = ((startMonth - 1 + i * monthInterval) % 12) + 1;

                lines.Add(new WorkPlanLine
                {
                    WorkPlanId = workPlanId,
                    ActivityId = activity.Id,
                    PeriodId = periodId,
                    Year = year,
                    Month = month,
                    Status = PlanLineStatus.Planned,
                    ApprovalStatus = ApprovalStatus.Draft,
                    CompanyId = companyId,
                    IsActive = true
                });
            }
        }

        return lines;
    }
}
