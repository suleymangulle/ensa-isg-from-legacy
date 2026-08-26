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
/// Year-end review report endpoints — <c>api/year-end-review-report</c>.
/// <para>
/// The <c>detail</c> route returns the work items as a tree; the flat <c>lines</c> route is for
/// editing. These routes store and retrieve report records and do not compute report content.
/// </para>
/// </summary>
public class YearEndReviewReportController(
    IYearEndReviewReportAppService yearEndReviewReportAppService)
    : EnsaController
{
    /// <summary>Returns a single report header.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.Report.Default)]
    [ProducesResponseType<YearEndReviewReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<YearEndReviewReportDto> GetAsync(int id, CancellationToken cancellationToken)
        => yearEndReviewReportAppService.GetAsync(id, cancellationToken);

    /// <summary>The report with its workplace and the complete work item tree.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.Report.Default)]
    [ProducesResponseType<YearEndReviewReportNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<YearEndReviewReportNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken)
        => yearEndReviewReportAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable report list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Report.Default)]
    [ProducesResponseType<PagedResultDto<YearEndReviewReportListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<YearEndReviewReportListDto>> GetListAsync(
        [FromQuery] GetYearEndReviewReportListInput input,
        CancellationToken cancellationToken)
        => yearEndReviewReportAppService.GetListAsync(input, cancellationToken);

    /// <summary>The most recent report of a workplace.</summary>
    [HttpGet("company/{companyId:int}/current")]
    [Authorize(EnsaPermissions.Report.Default)]
    [ProducesResponseType<YearEndReviewReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<YearEndReviewReportDto?> GetCurrentAsync(int companyId, CancellationToken cancellationToken)
        => yearEndReviewReportAppService.GetCurrentAsync(companyId, cancellationToken);

    /// <summary>Creates a report header.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.Report.Create)]
    [ProducesResponseType<YearEndReviewReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<YearEndReviewReportDto> CreateAsync(
        [FromBody] CreateYearEndReviewReportDto input,
        CancellationToken cancellationToken)
        => yearEndReviewReportAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates a report header.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.Report.Update)]
    [ProducesResponseType<YearEndReviewReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<YearEndReviewReportDto> UpdateAsync(
        int id,
        [FromBody] UpdateYearEndReviewReportDto input,
        CancellationToken cancellationToken)
        => yearEndReviewReportAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the report together with every work item in its tree.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.Report.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => yearEndReviewReportAppService.DeleteAsync(id, cancellationToken);

    // ------------------------------------------------------------ Work items

    /// <summary>Every work item of the report, flat.</summary>
    [HttpGet("{id:int}/lines")]
    [Authorize(EnsaPermissions.Report.Default)]
    [ProducesResponseType<ListResultDto<YearEndReviewLineDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<YearEndReviewLineDto>> GetLinesAsync(int id, CancellationToken cancellationToken)
        => yearEndReviewReportAppService.GetLinesAsync(id, cancellationToken);

    /// <summary>Adds a work item, optionally underneath a parent in the same report.</summary>
    [HttpPost("{id:int}/lines")]
    [Authorize(EnsaPermissions.Report.Create)]
    [ProducesResponseType<YearEndReviewLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<YearEndReviewLineDto> AddLineAsync(
        int id,
        [FromBody] CreateYearEndReviewLineDto input,
        CancellationToken cancellationToken)
        => yearEndReviewReportAppService.AddLineAsync(id, input, cancellationToken);

    /// <summary>Updates a work item. Re-parenting is checked against cycles.</summary>
    [HttpPut("{id:int}/lines/{lineId:int}")]
    [Authorize(EnsaPermissions.Report.Update)]
    [ProducesResponseType<YearEndReviewLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<YearEndReviewLineDto> UpdateLineAsync(
        int id,
        int lineId,
        [FromBody] UpdateYearEndReviewLineDto input,
        CancellationToken cancellationToken)
        => yearEndReviewReportAppService.UpdateLineAsync(id, lineId, input, cancellationToken);

    /// <summary>Removes a work item together with its whole subtree.</summary>
    [HttpDelete("{id:int}/lines/{lineId:int}")]
    [Authorize(EnsaPermissions.Report.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RemoveLineAsync(int id, int lineId, CancellationToken cancellationToken)
        => yearEndReviewReportAppService.RemoveLineAsync(id, lineId, cancellationToken);
}
