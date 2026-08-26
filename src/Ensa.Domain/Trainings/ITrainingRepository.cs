using Ensa.Domain.Trainings.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Trainings;

/// <summary>
/// Module-specific repository contract for <see cref="Training"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface ITrainingRepository : IRepository<Training>
{
    /// <summary>
    /// Checks that the training code is unique within the organization.
    /// (<paramref name="exceptTrainingId"/> is excluded, for the update case.)
    /// </summary>
    Task<bool> CodeExistsAsync(string trainingCode, int? tenantId, int? exceptTrainingId = null, CancellationToken cancellationToken = default);

    /// <summary>Loads the training as a combined view with its group, durations, topics and exams.</summary>
    Task<TrainingNavigation?> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The default, mandatory trainings a company must run, given its employee count and the
    /// applicable requirements. (Legacy: the query filtered on <c>Egitim_T.DefaultEgitim</c>.)
    /// </summary>
    Task<List<Training>> GetDefaultTrainingsAsync(int? tenantId, CancellationToken cancellationToken = default);

    /// <summary>Returns a training's durations per hazard class.</summary>
    Task<List<TrainingDuration>> GetDurationsAsync(int trainingId, CancellationToken cancellationToken = default);

    /// <summary>Returns a training's topics, in order.</summary>
    Task<List<TrainingTopic>> GetSubjectsAsync(int trainingId, CancellationToken cancellationToken = default);
}
