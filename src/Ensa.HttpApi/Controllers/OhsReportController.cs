using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Reports;
using Ensa.Application.Contracts.Reports.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// OHS service-time report endpoints — <c>api/ohs-report</c>.
/// <para>
/// Read-only: these records are produced by the reporting engine, so there is deliberately no
/// create, update or delete route. See <see cref="IOhsReportAppService"/>.
/// </para>
/// </summary>
public class OhsReportController(IOhsReportAppService ohsReportAppService) : EnsaController
{
    /// <summary>Returns a single OHS report.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<OhsReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OhsReportDto> GetAsync(int id, CancellationToken cancellationToken)
        => ohsReportAppService.GetAsync(id, cancellationToken);

    /// <summary>Paged, filterable OHS report list.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<OhsReportListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<OhsReportListDto>> GetListAsync(
        [FromQuery] GetOhsReportListInput input,
        CancellationToken cancellationToken)
        => ohsReportAppService.GetListAsync(input, cancellationToken);

    /// <summary>Reports of one office in a period.</summary>
    [HttpGet("office/{officeId:int}")]
    [ProducesResponseType<ListResultDto<OhsReportDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ListResultDto<OhsReportDto>> GetOfficeReportsAsync(
        int officeId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
        => ohsReportAppService.GetOfficeReportsAsync(officeId, from, to, cancellationToken);

    /// <summary>
    /// Hazard-class distribution of the workplaces covered by a report. Every class is returned,
    /// with zero where the report has no row.
    /// </summary>
    [HttpGet("{id:int}/hazard-class-breakdown")]
    [ProducesResponseType<ListResultDto<OhsReportHazardClassBreakdownDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<OhsReportHazardClassBreakdownDto>> GetHazardClassBreakdownAsync(
        int id,
        CancellationToken cancellationToken)
        => ohsReportAppService.GetHazardClassBreakdownAsync(id, cancellationToken);
}
