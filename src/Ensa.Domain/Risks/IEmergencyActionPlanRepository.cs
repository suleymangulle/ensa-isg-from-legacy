using Ensa.Domain.Repositories;
using Ensa.Domain.Risks.Navigations;

namespace Ensa.Domain.Risks;

/// <summary>
/// Module-specific repository contract for <see cref="EmergencyActionPlan"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IEmergencyActionPlanRepository : IRepository<EmergencyActionPlan>
{
    /// <summary>
    /// Loads the plan as a combined view with its company, its two documents, its sections and
    /// its team members (each with the assigned employee).
    /// <para>
    /// The projection belongs here rather than in the application service: the architecture puts
    /// combined reads in the repository so the query count stays fixed and visible in one place.
    /// </para>
    /// </summary>
    Task<EmergencyActionPlanNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);
}
