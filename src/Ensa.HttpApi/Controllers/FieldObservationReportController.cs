using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Risks;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>Field observation report endpoints — <c>api/field-observation-report</c>.</summary>
public class FieldObservationReportController(IFieldObservationReportAppService appService) : EnsaController
{
    /// <summary>Returns a single field observation report header.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<FieldObservationReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<FieldObservationReportDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>Combined detail view: report, department and every line with its related records.</summary>
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType<FieldObservationReportNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<FieldObservationReportNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken)
        => appService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable field observation report list.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<FieldObservationReportListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<FieldObservationReportListDto>> GetListAsync(
        [FromQuery] GetFieldObservationReportListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);

    /// <summary>Creates a new field observation report.</summary>
    [HttpPost]
    [ProducesResponseType<FieldObservationReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<FieldObservationReportDto> CreateAsync(
        [FromBody] CreateFieldObservationReportDto input,
        CancellationToken cancellationToken)
        => appService.CreateAsync(input, cancellationToken);

    /// <summary>Updates an existing field observation report.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<FieldObservationReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<FieldObservationReportDto> UpdateAsync(
        int id,
        [FromBody] UpdateFieldObservationReportDto input,
        CancellationToken cancellationToken)
        => appService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the report together with its lines (soft delete).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => appService.DeleteAsync(id, cancellationToken);

    // ------------------------------------------------------------------- Lines

    /// <summary>Non-conformity lines of a report.</summary>
    [HttpGet("{id:int}/lines")]
    [ProducesResponseType<ListResultDto<FieldObservationLineDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<FieldObservationLineDto>> GetLinesAsync(int id, CancellationToken cancellationToken)
        => appService.GetLinesAsync(id, cancellationToken);

    /// <summary>Adds a non-conformity line to the report.</summary>
    [HttpPost("{id:int}/lines")]
    [ProducesResponseType<FieldObservationLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<FieldObservationLineDto> AddLineAsync(
        int id,
        [FromBody] CreateFieldObservationLineDto input,
        CancellationToken cancellationToken)
        => appService.AddLineAsync(id, input, cancellationToken);

    /// <summary>Updates a non-conformity line.</summary>
    [HttpPut("{id:int}/lines/{lineId:int}")]
    [ProducesResponseType<FieldObservationLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<FieldObservationLineDto> UpdateLineAsync(
        int id,
        int lineId,
        [FromBody] UpdateFieldObservationLineDto input,
        CancellationToken cancellationToken)
        => appService.UpdateLineAsync(id, lineId, input, cancellationToken);

    /// <summary>Removes a line; refused while corrective actions are derived from it.</summary>
    [HttpDelete("{id:int}/lines/{lineId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task RemoveLineAsync(int id, int lineId, CancellationToken cancellationToken)
        => appService.RemoveLineAsync(id, lineId, cancellationToken);
}
