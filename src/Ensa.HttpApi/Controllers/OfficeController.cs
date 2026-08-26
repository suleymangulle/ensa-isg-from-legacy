using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Tenancy;
using Ensa.Application.Contracts.Tenancy.Dtos;
using Ensa.Application.Contracts.Tenancy.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Office endpoints — <c>api/office</c>.
/// <para>
/// Authorization is enforced by policy; error shaping is done by
/// <c>EnsaExceptionFilter</c>, so there is no <c>try/catch</c> here.
/// </para>
/// </summary>
public class OfficeController(IOfficeAppService officeAppService) : EnsaController
{
    /// <summary>Returns a single office record.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.Office.Default)]
    [ProducesResponseType<OfficeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OfficeDto> GetAsync(int id, CancellationToken cancellationToken)
        => officeAppService.GetAsync(id, cancellationToken);

    /// <summary>Combined view for the detail screen (organization, location, counters).</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.Office.Default)]
    [ProducesResponseType<OfficeNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OfficeNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => officeAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable office list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Office.Default)]
    [ProducesResponseType<PagedResultDto<OfficeListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<OfficeListDto>> GetListAsync(
        [FromQuery] GetOfficeListInput input,
        CancellationToken cancellationToken)
        => officeAppService.GetListAsync(input, cancellationToken);

    /// <summary>Lightweight records for drop-down lists (at most 50).</summary>
    [HttpGet("lookup")]
    [Authorize(EnsaPermissions.Office.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetLookupAsync(
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
        => officeAppService.GetLookupAsync(filter, cancellationToken);

    /// <summary>Creates a new office; refused when a headquarters office already exists.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.Office.Create)]
    [ProducesResponseType<OfficeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<OfficeDto> CreateAsync(
        [FromBody] CreateOfficeDto input,
        CancellationToken cancellationToken)
        => officeAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates an existing office; refused when another office is the headquarters.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.Office.Update)]
    [ProducesResponseType<OfficeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OfficeDto> UpdateAsync(
        int id,
        [FromBody] UpdateOfficeDto input,
        CancellationToken cancellationToken)
        => officeAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the office (soft delete).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.Office.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => officeAppService.DeleteAsync(id, cancellationToken);
}
