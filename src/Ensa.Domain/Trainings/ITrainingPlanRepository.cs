using Ensa.Domain.Trainings.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Trainings;

/// <summary>
/// Module-specific repository contract for <see cref="TrainingPlan"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface ITrainingPlanRepository : IRepository<TrainingPlan>
{
    /// <summary>
    /// Returns a company's active training plan for a given year.
    /// In legacy the <c>Active</c> column was derived from whether the plan's <c>StartDate.Year</c>
    /// matched the year it was created in.
    /// </summary>
    Task<TrainingPlan?> GetActivePlanAsync(int companyId, int year, CancellationToken cancellationToken = default);

    /// <summary>Loads the plan as a combined view with its company, OHS specialist, physician and lines, including training names and documents.</summary>
    Task<TrainingPlanNavigation?> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the lines of a training plan that have not yet reached
    /// <see cref="Shared.Enums.PlanLineStatus.Completed"/>, for the reminder and escalation screens.
    /// </summary>
    Task<List<TrainingPlanLine>> GetIncompleteLinesAsync(int trainingPlanId, CancellationToken cancellationToken = default);

    /// <summary>Returns every line of a training plan.</summary>
    Task<List<TrainingPlanLine>> GetLinesAsync(int trainingPlanId, CancellationToken cancellationToken = default);
}
