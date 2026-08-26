using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication;
using Ensa.Application.Contracts.Communication.Dtos;
using Ensa.Application.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>Visit endpoints — <c>api/visit</c>.</summary>
public class VisitController(IVisitAppService visitAppService) : EnsaController
{
    /// <summary>Returns a single visit.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.Visit.Default)]
    [ProducesResponseType<VisitDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<VisitDto> GetAsync(int id, CancellationToken cancellationToken)
        => visitAppService.GetAsync(id, cancellationToken);

    /// <summary>Paged, filterable visit list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Visit.Default)]
    [ProducesResponseType<PagedResultDto<VisitListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<VisitListDto>> GetListAsync(
        [FromQuery] GetVisitListInput input,
        CancellationToken cancellationToken)
        => visitAppService.GetListAsync(input, cancellationToken);

    /// <summary>
    /// Visits in a date range, shaped for a calendar UI. The range is capped so a single request
    /// cannot ask for the organization's entire visit history.
    /// </summary>
    [HttpGet("calendar")]
    [Authorize(EnsaPermissions.Visit.Default)]
    [ProducesResponseType<ListResultDto<VisitCalendarDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ListResultDto<VisitCalendarDto>> GetCalendarAsync(
        [FromQuery] int? userId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
        => visitAppService.GetCalendarAsync(userId, from, to, cancellationToken);

    /// <summary>Creates a visit. Defaults the visiting user to the caller.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.Visit.Create)]
    [ProducesResponseType<VisitDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<VisitDto> CreateAsync(
        [FromBody] CreateVisitDto input,
        CancellationToken cancellationToken)
        => visitAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates a visit.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.Visit.Update)]
    [ProducesResponseType<VisitDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<VisitDto> UpdateAsync(
        int id,
        [FromBody] UpdateVisitDto input,
        CancellationToken cancellationToken)
        => visitAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes a visit (soft delete).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.Visit.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => visitAppService.DeleteAsync(id, cancellationToken);
}
