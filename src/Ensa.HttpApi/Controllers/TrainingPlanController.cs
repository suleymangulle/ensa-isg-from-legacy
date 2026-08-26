using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Trainings;
using Ensa.Application.Contracts.Trainings.Dtos;
using Ensa.Application.Contracts.Trainings.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Annual training plan endpoints — <c>api/training-plan</c>. Header, lines and the per-line
/// approval workflow.
/// </summary>
public class TrainingPlanController(ITrainingPlanAppService appService) : EnsaController
{
    /// <summary>Returns one plan header.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.TrainingPlan.Default)]
    [ProducesResponseType<TrainingPlanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingPlanDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>Plan with the workplace, the staff and every line.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.TrainingPlan.Default)]
    [ProducesResponseType<TrainingPlanNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingPlanNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => appService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable plan list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.TrainingPlan.Default)]
    [ProducesResponseType<PagedResultDto<TrainingPlanListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<TrainingPlanListDto>> GetListAsync(
        [FromQuery] GetTrainingPlanListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);

    /// <summary>The workplace's plan in force for the given year.</summary>
    [HttpGet("active")]
    [Authorize(EnsaPermissions.TrainingPlan.Default)]
    [ProducesResponseType<TrainingPlanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<TrainingPlanDto?> GetActivePlanAsync(
        [FromQuery] int companyId,
        [FromQuery] int year,
        CancellationToken cancellationToken)
        => appService.GetActivePlanAsync(companyId, year, cancellationToken);

    /// <summary>Lines that have not yet reached <c>Completed</c>.</summary>
    [HttpGet("{id:int}/incomplete-lines")]
    [Authorize(EnsaPermissions.TrainingPlan.Default)]
    [ProducesResponseType<ListResultDto<TrainingPlanLineDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<TrainingPlanLineDto>> GetIncompleteLinesAsync(
        int id,
        CancellationToken cancellationToken)
        => appService.GetIncompleteLinesAsync(id, cancellationToken);

    /// <summary>Creates a plan header.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.TrainingPlan.Create)]
    [ProducesResponseType<TrainingPlanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<TrainingPlanDto> CreateAsync(
        [FromBody] CreateTrainingPlanDto input,
        CancellationToken cancellationToken)
        => appService.CreateAsync(input, cancellationToken);

    /// <summary>Updates a plan header.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.TrainingPlan.Update)]
    [ProducesResponseType<TrainingPlanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingPlanDto> UpdateAsync(
        int id,
        [FromBody] UpdateTrainingPlanDto input,
        CancellationToken cancellationToken)
        => appService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes a plan that carries no approved line.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.TrainingPlan.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => appService.DeleteAsync(id, cancellationToken);

    // ------------------------------------------------------------------ Lines

    /// <summary>
    /// Paged list of plan lines across every plan, with company, training and instructor names
    /// resolved. This is the operational training-plan screen; the nested route below serves a
    /// single plan's lines.
    /// </summary>
    [HttpGet("lines")]
    [Authorize(EnsaPermissions.TrainingPlan.Default)]
    [ProducesResponseType<PagedResultDto<TrainingPlanLineListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<TrainingPlanLineListDto>> GetLineListAsync(
        [FromQuery] GetTrainingPlanLineListInput input,
        CancellationToken cancellationToken)
        => appService.GetLineListAsync(input, cancellationToken);

    /// <summary>All lines of a plan.</summary>
    [HttpGet("{id:int}/lines")]
    [Authorize(EnsaPermissions.TrainingPlan.Default)]
    [ProducesResponseType<ListResultDto<TrainingPlanLineDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<TrainingPlanLineDto>> GetLinesAsync(int id, CancellationToken cancellationToken)
        => appService.GetLinesAsync(id, cancellationToken);

    /// <summary>Adds a line to a plan.</summary>
    [HttpPost("{id:int}/lines")]
    [Authorize(EnsaPermissions.TrainingPlan.Create)]
    [ProducesResponseType<TrainingPlanLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingPlanLineDto> AddLineAsync(
        int id,
        [FromBody] CreateTrainingPlanLineDto input,
        CancellationToken cancellationToken)
        => appService.AddLineAsync(id, input, cancellationToken);

    /// <summary>Updates a line.</summary>
    [HttpPut("{id:int}/lines/{lineId:int}")]
    [Authorize(EnsaPermissions.TrainingPlan.Update)]
    [ProducesResponseType<TrainingPlanLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingPlanLineDto> UpdateLineAsync(
        int id,
        int lineId,
        [FromBody] UpdateTrainingPlanLineDto input,
        CancellationToken cancellationToken)
        => appService.UpdateLineAsync(id, lineId, input, cancellationToken);

    /// <summary>Removes a line.</summary>
    [HttpDelete("{id:int}/lines/{lineId:int}")]
    [Authorize(EnsaPermissions.TrainingPlan.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RemoveLineAsync(int id, int lineId, CancellationToken cancellationToken)
        => appService.RemoveLineAsync(id, lineId, cancellationToken);

    // --------------------------------------------------------- Approval flow

    /// <summary>Submits a line for approval.</summary>
    [HttpPost("{id:int}/lines/{lineId:int}/submit")]
    [Authorize(EnsaPermissions.TrainingPlan.Update)]
    [ProducesResponseType<TrainingPlanLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingPlanLineDto> SubmitLineForApprovalAsync(
        int id,
        int lineId,
        CancellationToken cancellationToken)
        => appService.SubmitLineForApprovalAsync(id, lineId, cancellationToken);

    /// <summary>Approves a line that is awaiting approval.</summary>
    [HttpPost("{id:int}/lines/{lineId:int}/approve")]
    [Authorize(EnsaPermissions.TrainingPlan.Approve)]
    [ProducesResponseType<TrainingPlanLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingPlanLineDto> ApproveLineAsync(int id, int lineId, CancellationToken cancellationToken)
        => appService.ApproveLineAsync(id, lineId, cancellationToken);

    /// <summary>Rejects a line that is awaiting approval, recording the reason.</summary>
    [HttpPost("{id:int}/lines/{lineId:int}/reject")]
    [Authorize(EnsaPermissions.TrainingPlan.Approve)]
    [ProducesResponseType<TrainingPlanLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingPlanLineDto> RejectLineAsync(
        int id,
        int lineId,
        [FromBody] RejectPlanLineDto input,
        CancellationToken cancellationToken)
        => appService.RejectLineAsync(id, lineId, input.Reason, cancellationToken);
}
