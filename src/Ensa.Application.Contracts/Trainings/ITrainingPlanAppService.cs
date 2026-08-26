using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Trainings.Dtos;
using Ensa.Application.Contracts.Trainings.Dtos.Navigations;

namespace Ensa.Application.Contracts.Trainings;

/// <summary>
/// Annual training plan application service — header plus lines.
/// </summary>
public interface ITrainingPlanAppService : IApplicationService
{
    Task<TrainingPlanDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Plan with the workplace, the specialist and physician, and all its lines.</summary>
    Task<TrainingPlanNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<TrainingPlanListDto>> GetListAsync(
        GetTrainingPlanListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>The workplace's plan in force for the given year, or <c>null</c> when there is none.</summary>
    Task<TrainingPlanDto?> GetActivePlanAsync(
        int companyId,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>Lines of a plan that have not yet reached <c>Completed</c> — the follow-up list.</summary>
    Task<ListResultDto<TrainingPlanLineDto>> GetIncompleteLinesAsync(
        int planId,
        CancellationToken cancellationToken = default);

    Task<TrainingPlanDto> CreateAsync(CreateTrainingPlanDto input, CancellationToken cancellationToken = default);

    Task<TrainingPlanDto> UpdateAsync(
        int id,
        UpdateTrainingPlanDto input,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ------------------------------------------------------------------ Lines

    /// <summary>All lines of a plan.</summary>
    /// <summary>
    /// Paged, filterable list of plan lines across every plan, with company, training and
    /// instructor names resolved. This is what the training plan screen lists.
    /// </summary>
    Task<PagedResultDto<TrainingPlanLineListDto>> GetLineListAsync(
        GetTrainingPlanLineListInput input,
        CancellationToken cancellationToken = default);

    Task<ListResultDto<TrainingPlanLineDto>> GetLinesAsync(
        int planId,
        CancellationToken cancellationToken = default);

    Task<TrainingPlanLineDto> AddLineAsync(
        int planId,
        CreateTrainingPlanLineDto input,
        CancellationToken cancellationToken = default);

    Task<TrainingPlanLineDto> UpdateLineAsync(
        int planId,
        int lineId,
        UpdateTrainingPlanLineDto input,
        CancellationToken cancellationToken = default);

    Task RemoveLineAsync(int planId, int lineId, CancellationToken cancellationToken = default);

    // --------------------------------------------------------- Approval flow
    // Draft → ForApprovalSent → Approved | Rejected; a rejected line may be resubmitted.

    /// <summary>Submits a line for approval.</summary>
    Task<TrainingPlanLineDto> SubmitLineForApprovalAsync(
        int planId,
        int lineId,
        CancellationToken cancellationToken = default);

    /// <summary>Approves a line that is awaiting approval.</summary>
    Task<TrainingPlanLineDto> ApproveLineAsync(
        int planId,
        int lineId,
        CancellationToken cancellationToken = default);

    /// <summary>Rejects a line that is awaiting approval, recording the reason.</summary>
    Task<TrainingPlanLineDto> RejectLineAsync(
        int planId,
        int lineId,
        string reason,
        CancellationToken cancellationToken = default);
}
