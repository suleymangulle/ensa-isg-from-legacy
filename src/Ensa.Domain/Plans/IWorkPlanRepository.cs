using Ensa.Domain.Plans.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Plans;

/// <summary>
/// Module-specific repository contract for <see cref="WorkPlan"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface IWorkPlanRepository : IRepository<WorkPlan>
{
    /// <summary>
    /// Returns a company's active work plan for a given year.
    /// In legacy the <c>Active</c> column was derived from whether the plan's <c>StartDate.Year</c>
    /// matched the year it was created in.
    /// </summary>
    Task<WorkPlan?> GetActivePlanAsync(int companyId, int year, CancellationToken cancellationToken = default);

    /// <summary>Loads the plan as a combined view with its company, OHS specialist, physician and lines, including activity names and documents.</summary>
    Task<WorkPlanNavigation?> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the lines waiting in <see cref="Shared.Enums.ApprovalStatus.SubmittedForApproval"/> —
    /// that is, lines neither approved nor rejected yet.
    /// </summary>
    Task<List<WorkPlanLine>> GetApprovalPendingLinesAsync(int workPlanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the fraction of a work plan's lines that have reached
    /// <see cref="Shared.Enums.PlanLineStatus.Completed"/>, as a ratio between 0 and 1.
    /// </summary>
    Task<double> GetCompletionRateAsync(int workPlanId, CancellationToken cancellationToken = default);

    /// <summary>Returns every line of a work plan.</summary>
    Task<List<WorkPlanLine>> GetLinesAsync(int workPlanId, CancellationToken cancellationToken = default);
}
