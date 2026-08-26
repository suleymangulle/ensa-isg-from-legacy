using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Plans;
using Ensa.Application.Contracts.Plans.Dtos;
using Ensa.Application.Contracts.Plans.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Annual OHS work plan endpoints — <c>api/work-plan</c>. Header, lines and the per-line
/// approval workflow.
/// </summary>
public class WorkPlanController(IWorkPlanAppService appService) : EnsaController
{
    /// <summary>Returns one plan header.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.WorkPlan.Default)]
    [ProducesResponseType<WorkPlanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkPlanDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>Plan with the workplace, the staff and every line.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.WorkPlan.Default)]
    [ProducesResponseType<WorkPlanNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkPlanNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => appService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable plan list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.WorkPlan.Default)]
    [ProducesResponseType<PagedResultDto<WorkPlanListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<WorkPlanListDto>> GetListAsync(
        [FromQuery] GetWorkPlanListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);

    /// <summary>The workplace's plan in force for the given year.</summary>
    [HttpGet("active")]
    [Authorize(EnsaPermissions.WorkPlan.Default)]
    [ProducesResponseType<WorkPlanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<WorkPlanDto?> GetActivePlanAsync(
        [FromQuery] int companyId,
        [FromQuery] int year,
        CancellationToken cancellationToken)
        => appService.GetActivePlanAsync(companyId, year, cancellationToken);

    /// <summary>Creates a plan header.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.WorkPlan.Create)]
    [ProducesResponseType<WorkPlanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<WorkPlanDto> CreateAsync(
        [FromBody] CreateWorkPlanDto input,
        CancellationToken cancellationToken)
        => appService.CreateAsync(input, cancellationToken);

    /// <summary>Updates a plan header.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.WorkPlan.Update)]
    [ProducesResponseType<WorkPlanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkPlanDto> UpdateAsync(
        int id,
        [FromBody] UpdateWorkPlanDto input,
        CancellationToken cancellationToken)
        => appService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes a plan that carries no approved line.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.WorkPlan.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => appService.DeleteAsync(id, cancellationToken);

    // ------------------------------------------------------------------ Lines

    /// <summary>All lines of a plan.</summary>
    [HttpGet("{id:int}/lines")]
    [Authorize(EnsaPermissions.WorkPlan.Default)]
    [ProducesResponseType<ListResultDto<WorkPlanLineDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<WorkPlanLineDto>> GetLinesAsync(int id, CancellationToken cancellationToken)
        => appService.GetLinesAsync(id, cancellationToken);

    /// <summary>Adds a line to a plan.</summary>
    [HttpPost("{id:int}/lines")]
    [Authorize(EnsaPermissions.WorkPlan.Create)]
    [ProducesResponseType<WorkPlanLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkPlanLineDto> AddLineAsync(
        int id,
        [FromBody] CreateWorkPlanLineDto input,
        CancellationToken cancellationToken)
        => appService.AddLineAsync(id, input, cancellationToken);

    /// <summary>Updates a line.</summary>
    [HttpPut("{id:int}/lines/{lineId:int}")]
    [Authorize(EnsaPermissions.WorkPlan.Update)]
    [ProducesResponseType<WorkPlanLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkPlanLineDto> UpdateLineAsync(
        int id,
        int lineId,
        [FromBody] UpdateWorkPlanLineDto input,
        CancellationToken cancellationToken)
        => appService.UpdateLineAsync(id, lineId, input, cancellationToken);

    /// <summary>Removes a line.</summary>
    [HttpDelete("{id:int}/lines/{lineId:int}")]
    [Authorize(EnsaPermissions.WorkPlan.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RemoveLineAsync(int id, int lineId, CancellationToken cancellationToken)
        => appService.RemoveLineAsync(id, lineId, cancellationToken);

    /// <summary>Fills an empty plan with lines generated from the default activities.</summary>
    [HttpPost("{id:int}/generate-default-lines")]
    [Authorize(EnsaPermissions.WorkPlan.Create)]
    [ProducesResponseType<ListResultDto<WorkPlanLineDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<WorkPlanLineDto>> GenerateDefaultLinesAsync(
        int id,
        [FromQuery] int year,
        CancellationToken cancellationToken)
        => appService.GenerateDefaultLinesAsync(id, year, cancellationToken);

    /// <summary>Share of the plan's lines that reached <c>Completed</c>.</summary>
    [HttpGet("{id:int}/completion-rate")]
    [Authorize(EnsaPermissions.WorkPlan.Default)]
    [ProducesResponseType<WorkPlanCompletionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkPlanCompletionDto> GetCompletionRateAsync(int id, CancellationToken cancellationToken)
        => appService.GetCompletionRateAsync(id, cancellationToken);

    // --------------------------------------------------------- Approval flow

    /// <summary>Submits a line for approval.</summary>
    [HttpPost("{id:int}/lines/{lineId:int}/submit")]
    [Authorize(EnsaPermissions.WorkPlan.Update)]
    [ProducesResponseType<WorkPlanLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkPlanLineDto> SubmitLineForApprovalAsync(
        int id,
        int lineId,
        CancellationToken cancellationToken)
        => appService.SubmitLineForApprovalAsync(id, lineId, cancellationToken);

    /// <summary>Approves a line that is awaiting approval.</summary>
    [HttpPost("{id:int}/lines/{lineId:int}/approve")]
    [Authorize(EnsaPermissions.WorkPlan.Approve)]
    [ProducesResponseType<WorkPlanLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkPlanLineDto> ApproveLineAsync(int id, int lineId, CancellationToken cancellationToken)
        => appService.ApproveLineAsync(id, lineId, cancellationToken);

    /// <summary>Rejects a line that is awaiting approval, recording the reason.</summary>
    [HttpPost("{id:int}/lines/{lineId:int}/reject")]
    [Authorize(EnsaPermissions.WorkPlan.Approve)]
    [ProducesResponseType<WorkPlanLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkPlanLineDto> RejectLineAsync(
        int id,
        int lineId,
        [FromBody] RejectWorkPlanLineDto input,
        CancellationToken cancellationToken)
        => appService.RejectLineAsync(id, lineId, input.Reason, cancellationToken);
}
