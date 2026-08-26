using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Trainings.Dtos;
using Ensa.Application.Contracts.Trainings.Dtos.Navigations;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Trainings;

/// <summary>
/// Training catalogue application service.
/// <para>
/// Durations are exposed as a normalised list of <c>{ hazardClass, durationMinutes }</c>
/// pairs — never as three flat columns — for both the training itself and each of its topics.
/// </para>
/// <para>
/// The statutory refresh interval (low 3 years, hazardous 2 years, very hazardous 1 year),
/// the mandatory duration (480 / 720 / 960 minutes) and the validity check are owned by
/// <c>ITrainingPlanningManager</c>; this service calls it and never re-implements the rules.
/// </para>
/// </summary>
public interface ITrainingAppService : IApplicationService
{
    Task<TrainingDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Training with its group, durations, topics (with their durations) and exams.</summary>
    Task<TrainingNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<TrainingListDto>> GetListAsync(
        GetTrainingListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Lightweight records for drop-down lists.</summary>
    Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>Trainings marked as mandatory defaults, used when generating a training plan.</summary>
    Task<ListResultDto<TrainingListDto>> GetDefaultsAsync(
        int? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates the training together with its hazard-class durations.</summary>
    Task<TrainingDto> CreateAsync(CreateTrainingDto input, CancellationToken cancellationToken = default);

    /// <summary>Updates the training and replaces its duration set.</summary>
    Task<TrainingDto> UpdateAsync(int id, UpdateTrainingDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ------------------------------------------------------------------ Topics

    /// <summary>Topics of a training in display order, each with its hazard-class durations.</summary>
    Task<ListResultDto<TrainingTopicDto>> GetTopicsAsync(
        int trainingId,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a topic to a training together with its durations.</summary>
    Task<TrainingTopicDto> CreateTopicAsync(
        int trainingId,
        CreateTrainingTopicDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Updates a topic and replaces its duration set.</summary>
    Task<TrainingTopicDto> UpdateTopicAsync(
        int trainingId,
        int topicId,
        UpdateTrainingTopicDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a topic and its durations from a training.</summary>
    Task DeleteTopicAsync(int trainingId, int topicId, CancellationToken cancellationToken = default);

    // ------------------------------------------------- Statutory calculations

    /// <summary>
    /// Whether an employee's training is still valid, together with the mandatory duration
    /// for the hazard class. Both answers come from <c>ITrainingPlanningManager</c>.
    /// </summary>
    Task<TrainingValidityDto> GetValidityAsync(
        int companyEmployeeId,
        int trainingId,
        HazardClass hazardClass,
        CancellationToken cancellationToken = default);
}
