using Ensa.Domain.Trainings.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Trainings;

/// <summary>
/// Module-specific repository contract for <see cref="EmployeeTrainingProgress"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface IEmployeeTrainingProgressRepository : IRepository<EmployeeTrainingProgress>
{
    /// <summary>Returns an employee's progress record for a training, optionally scoped to a topic.</summary>
    Task<EmployeeTrainingProgress?> FindAsync(int companyEmployeeId, int trainingId, int? trainingTopicId, CancellationToken cancellationToken = default);

    /// <summary>Returns the combined view of the progress, employee and training, including the remaining duration.</summary>
    Task<EmployeeTrainingProgressNavigation?> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns an employee's progress records across every training.</summary>
    Task<List<EmployeeTrainingProgress>> GetEmployeeProgressAsync(int companyEmployeeId, CancellationToken cancellationToken = default);
}
