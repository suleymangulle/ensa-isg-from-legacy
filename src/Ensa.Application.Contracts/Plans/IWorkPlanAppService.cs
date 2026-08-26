using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Plans.Dtos;
using Ensa.Application.Contracts.Plans.Dtos.Navigations;

namespace Ensa.Application.Contracts.Plans;

/// <summary>
/// Annual occupational health and safety work plan application service — header plus lines.
/// <para>
/// The "one active plan per company and year" rule, the approval state machine
/// (<c>Draft → ForApprovalSent → Approved | Rejected</c>) and the generation of default
/// lines from the activity catalogue are owned by <c>IWorkPlanManager</c>; this service
/// calls it and never re-implements the rules.
/// </para>
/// </summary>
public interface IWorkPlanAppService : IApplicationService
{
    Task<WorkPlanDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Plan with the workplace, the specialist and physician, and all its lines.</summary>
    Task<WorkPlanNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<WorkPlanListDto>> GetListAsync(
        GetWorkPlanListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>The workplace's plan in force for the given year, or <c>null</c> when there is none.</summary>
    Task<WorkPlanDto?> GetActivePlanAsync(int companyId, int year, CancellationToken cancellationToken = default);

    Task<WorkPlanDto> CreateAsync(CreateWorkPlanDto input, CancellationToken cancellationToken = default);

    Task<WorkPlanDto> UpdateAsync(int id, UpdateWorkPlanDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ------------------------------------------------------------------ Lines

    Task<ListResultDto<WorkPlanLineDto>> GetLinesAsync(int planId, CancellationToken cancellationToken = default);

    Task<WorkPlanLineDto> AddLineAsync(
        int planId,
        CreateWorkPlanLineDto input,
        CancellationToken cancellationToken = default);

    Task<WorkPlanLineDto> UpdateLineAsync(
        int planId,
        int lineId,
        UpdateWorkPlanLineDto input,
        CancellationToken cancellationToken = default);

    Task RemoveLineAsync(int planId, int lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fills the plan with lines generated from the default activities of the catalogue,
    /// spread across the year by <c>IWorkPlanManager.GenerateDefaultLines</c>. Only runs on a
    /// plan that has no lines yet.
    /// </summary>
    Task<ListResultDto<WorkPlanLineDto>> GenerateDefaultLinesAsync(
        int planId,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>Share of the plan's lines that reached <c>Completed</c>.</summary>
    Task<WorkPlanCompletionDto> GetCompletionRateAsync(int planId, CancellationToken cancellationToken = default);

    // --------------------------------------------------------- Approval flow

    /// <summary>Submits a line for approval.</summary>
    Task<WorkPlanLineDto> SubmitLineForApprovalAsync(
        int planId,
        int lineId,
        CancellationToken cancellationToken = default);

    /// <summary>Approves a line that is awaiting approval.</summary>
    Task<WorkPlanLineDto> ApproveLineAsync(
        int planId,
        int lineId,
        CancellationToken cancellationToken = default);

    /// <summary>Rejects a line that is awaiting approval, recording the reason.</summary>
    Task<WorkPlanLineDto> RejectLineAsync(
        int planId,
        int lineId,
        string reason,
        CancellationToken cancellationToken = default);
}
