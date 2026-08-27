using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Membership;
using Ensa.Application.Contracts.Membership.Dtos;
using Ensa.Application.Contracts.Membership.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Permission catalogue and per-user assignment endpoints - <c>api/permission</c>.
/// <para>
/// The catalogue is seeded from the <c>EnsaPermissions</c> constants, so it is read-only over
/// HTTP: there is no POST or DELETE for permission definitions, only for user assignment.
/// </para>
/// </summary>
public class PermissionController(IPermissionAppService permissionAppService) : EnsaController
{
    /// <summary>Paged, filterable permission list.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<PermissionDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<PermissionDto>> GetListAsync(
        [FromQuery] GetPermissionListInput input,
        CancellationToken cancellationToken)
        => permissionAppService.GetListAsync(input, cancellationToken);

    /// <summary>The whole catalogue as a hierarchy, for the permission assignment screen.</summary>
    [HttpGet("tree")]
    [ProducesResponseType<PermissionTreeDto>(StatusCodes.Status200OK)]
    public Task<PermissionTreeDto> GetTreeAsync(CancellationToken cancellationToken)
        => permissionAppService.GetTreeAsync(cancellationToken);

    /// <summary>Effective permissions of one user plus the explicit grant/deny overrides.</summary>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType<UserPermissionsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<UserPermissionsDto> GetUserPermissionsAsync(
        int userId,
        CancellationToken cancellationToken)
        => permissionAppService.GetUserPermissionsAsync(userId, cancellationToken);

    /// <summary>Replaces the explicit grant/deny overrides of one user.</summary>
    [HttpPut("user/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task SaveUserPermissionsAsync(
        int userId,
        [FromBody] UpdateUserPermissionsDto input,
        CancellationToken cancellationToken)
        => permissionAppService.SaveUserPermissionsAsync(userId, input, cancellationToken);
    /// <summary>
    /// Permission defaults of one staff type — what every user of that type can do without a
    /// per-user grant.
    /// </summary>
    [HttpGet("user-type/{userTypeId:int}")]
    [ProducesResponseType<UserTypePermissionsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<UserTypePermissionsDto> GetUserTypePermissionsAsync(
        int userTypeId,
        CancellationToken cancellationToken)
        => permissionAppService.GetUserTypePermissionsAsync(userTypeId, cancellationToken);

    /// <summary>Replaces the permission defaults of a staff type. The list is absolute.</summary>
    [HttpPut("user-type/{userTypeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveUserTypePermissionsAsync(
        int userTypeId,
        [FromBody] UpdateUserTypePermissionsDto input,
        CancellationToken cancellationToken)
    {
        await permissionAppService.SaveUserTypePermissionsAsync(userTypeId, input, cancellationToken);
        return NoContent();
    }
}
