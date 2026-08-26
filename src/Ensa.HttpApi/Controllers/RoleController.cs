using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Membership;
using Ensa.Application.Contracts.Membership.Dtos;
using Ensa.Application.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>Role endpoints - <c>api/role</c>.</summary>
public class RoleController(IRoleAppService roleAppService) : EnsaController
{
    /// <summary>Returns a single role together with its member count.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.Role.Default)]
    [ProducesResponseType<RoleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<RoleDto> GetAsync(int id, CancellationToken cancellationToken)
        => roleAppService.GetAsync(id, cancellationToken);

    /// <summary>Paged, filterable role list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Role.Default)]
    [ProducesResponseType<PagedResultDto<RoleListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<RoleListDto>> GetListAsync(
        [FromQuery] GetRoleListInput input,
        CancellationToken cancellationToken)
        => roleAppService.GetListAsync(input, cancellationToken);

    /// <summary>Lightweight records for role pickers (at most 50).</summary>
    [HttpGet("lookup")]
    [Authorize(EnsaPermissions.Role.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetLookupAsync(
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
        => roleAppService.GetLookupAsync(filter, cancellationToken);

    /// <summary>Creates a role.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.Role.Create)]
    [ProducesResponseType<RoleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<RoleDto> CreateAsync(
        [FromBody] CreateRoleDto input,
        CancellationToken cancellationToken)
        => roleAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates the role. System roles cannot be renamed.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.Role.Update)]
    [ProducesResponseType<RoleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<RoleDto> UpdateAsync(
        int id,
        [FromBody] UpdateRoleDto input,
        CancellationToken cancellationToken)
        => roleAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the role. System roles cannot be deleted.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.Role.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => roleAppService.DeleteAsync(id, cancellationToken);
}
