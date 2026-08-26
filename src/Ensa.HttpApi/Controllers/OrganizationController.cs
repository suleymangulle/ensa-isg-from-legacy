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
/// Organization (tenant) endpoints — <c>api/organization</c>.
/// <para>
/// The organization is a host record, so every endpoint requires
/// <c>EnsaPermissions.Tenant.*</c> and is meant for system administrators.
/// Error shaping is done by <c>EnsaExceptionFilter</c>; there is no <c>try/catch</c> here.
/// </para>
/// </summary>
public class OrganizationController(IOrganizationAppService organizationAppService) : EnsaController
{
    /// <summary>Returns a single organization record.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.Tenant.Default)]
    [ProducesResponseType<OrganizationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OrganizationDto> GetAsync(int id, CancellationToken cancellationToken)
        => organizationAppService.GetAsync(id, cancellationToken);

    /// <summary>Combined view for the detail screen (type, plan, location, offices, counters).</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.Tenant.Default)]
    [ProducesResponseType<OrganizationNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OrganizationNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => organizationAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable organization list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Tenant.Default)]
    [ProducesResponseType<PagedResultDto<OrganizationListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<OrganizationListDto>> GetListAsync(
        [FromQuery] GetOrganizationListInput input,
        CancellationToken cancellationToken)
        => organizationAppService.GetListAsync(input, cancellationToken);

    /// <summary>Lightweight records for drop-down lists (at most 50).</summary>
    [HttpGet("lookup")]
    [Authorize(EnsaPermissions.Tenant.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetLookupAsync(
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
        => organizationAppService.GetLookupAsync(filter, cancellationToken);

    /// <summary>Creates a new organization.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.Tenant.Create)]
    [ProducesResponseType<OrganizationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<OrganizationDto> CreateAsync(
        [FromBody] CreateOrganizationDto input,
        CancellationToken cancellationToken)
        => organizationAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates an existing organization.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.Tenant.Update)]
    [ProducesResponseType<OrganizationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OrganizationDto> UpdateAsync(
        int id,
        [FromBody] UpdateOrganizationDto input,
        CancellationToken cancellationToken)
        => organizationAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deactivates and soft-deletes the organization.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.Tenant.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => organizationAppService.DeleteAsync(id, cancellationToken);
}
