using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Reports;
using Ensa.Application.Contracts.Reports.Dtos;
using Ensa.Application.Contracts.Reports.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Activity report endpoints — <c>api/activity-report</c>.
/// <para>
/// These routes store and retrieve report records; they do not compute report content. See
/// <see cref="IActivityReportAppService"/>.
/// </para>
/// </summary>
public class ActivityReportController(IActivityReportAppService activityReportAppService) : EnsaController
{
    /// <summary>Returns a single report header.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.Report.Default)]
    [ProducesResponseType<ActivityReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActivityReportDto> GetAsync(int id, CancellationToken cancellationToken)
        => activityReportAppService.GetAsync(id, cancellationToken);

    /// <summary>The report with its workplace and its data rows.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.Report.Default)]
    [ProducesResponseType<ActivityReportNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActivityReportNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => activityReportAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable report list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Report.Default)]
    [ProducesResponseType<PagedResultDto<ActivityReportListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<ActivityReportListDto>> GetListAsync(
        [FromQuery] GetActivityReportListInput input,
        CancellationToken cancellationToken)
        => activityReportAppService.GetListAsync(input, cancellationToken);

    /// <summary>Creates a report header.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.Report.Create)]
    [ProducesResponseType<ActivityReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActivityReportDto> CreateAsync(
        [FromBody] CreateActivityReportDto input,
        CancellationToken cancellationToken)
        => activityReportAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates a report header.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.Report.Update)]
    [ProducesResponseType<ActivityReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActivityReportDto> UpdateAsync(
        int id,
        [FromBody] UpdateActivityReportDto input,
        CancellationToken cancellationToken)
        => activityReportAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the report together with its data rows.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.Report.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => activityReportAppService.DeleteAsync(id, cancellationToken);

    // ---------------------------------------------------------------- Lines

    /// <summary>Data rows of one report, in display order.</summary>
    [HttpGet("{id:int}/lines")]
    [Authorize(EnsaPermissions.Report.Default)]
    [ProducesResponseType<ListResultDto<ActivityReportLineDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<ActivityReportLineDto>> GetLinesAsync(int id, CancellationToken cancellationToken)
        => activityReportAppService.GetLinesAsync(id, cancellationToken);

    /// <summary>Adds a data row.</summary>
    [HttpPost("{id:int}/lines")]
    [Authorize(EnsaPermissions.Report.Create)]
    [ProducesResponseType<ActivityReportLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActivityReportLineDto> AddLineAsync(
        int id,
        [FromBody] CreateActivityReportLineDto input,
        CancellationToken cancellationToken)
        => activityReportAppService.AddLineAsync(id, input, cancellationToken);

    /// <summary>Updates a data row.</summary>
    [HttpPut("{id:int}/lines/{lineId:int}")]
    [Authorize(EnsaPermissions.Report.Update)]
    [ProducesResponseType<ActivityReportLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActivityReportLineDto> UpdateLineAsync(
        int id,
        int lineId,
        [FromBody] UpdateActivityReportLineDto input,
        CancellationToken cancellationToken)
        => activityReportAppService.UpdateLineAsync(id, lineId, input, cancellationToken);

    /// <summary>Removes a data row.</summary>
    [HttpDelete("{id:int}/lines/{lineId:int}")]
    [Authorize(EnsaPermissions.Report.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RemoveLineAsync(int id, int lineId, CancellationToken cancellationToken)
        => activityReportAppService.RemoveLineAsync(id, lineId, cancellationToken);
}
