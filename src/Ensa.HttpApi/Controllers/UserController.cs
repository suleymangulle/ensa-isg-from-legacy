using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Membership;
using Ensa.Application.Contracts.Membership.Dtos;
using Ensa.Application.Contracts.Membership.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Administrative user endpoints - <c>api/user</c>.
/// <para>
/// Self-service operations (own profile, own password) live on <c>api/account</c> instead.
/// </para>
/// </summary>
public class UserController(IUserAppService userAppService) : EnsaController
{
    /// <summary>Returns a single user.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<UserDto> GetAsync(int id, CancellationToken cancellationToken)
        => userAppService.GetAsync(id, cancellationToken);

    /// <summary>Combined detail view: organization, offices, roles and effective permissions.</summary>
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType<UserNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<UserNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => userAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable user list.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<UserListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<UserListDto>> GetListAsync(
        [FromQuery] GetUserListInput input,
        CancellationToken cancellationToken)
        => userAppService.GetListAsync(input, cancellationToken);

    /// <summary>Lightweight records for drop-downs (at most 50).</summary>
    [HttpGet("lookup")]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetLookupAsync(
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
        => userAppService.GetLookupAsync(filter, cancellationToken);

    /// <summary>Creates a user together with its initial password and roles.</summary>
    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<UserDto> CreateAsync(
        [FromBody] CreateUserDto input,
        CancellationToken cancellationToken)
        => userAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates the user. The payload carries no password and no user name.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<UserDto> UpdateAsync(
        int id,
        [FromBody] UpdateUserDto input,
        CancellationToken cancellationToken)
        => userAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the user (soft delete).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => userAppService.DeleteAsync(id, cancellationToken);

    /// <summary>
    /// Administrative password reset. Rotates the security stamp, so every outstanding
    /// refresh token of that user stops working.
    /// </summary>
    [HttpPost("{id:int}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task ResetPasswordAsync(
        int id,
        [FromBody] ResetPasswordDto input,
        CancellationToken cancellationToken)
        => userAppService.ResetPasswordAsync(id, input.NewPassword, cancellationToken);

    /// <summary>Replaces the complete role set of the user.</summary>
    [HttpPut("{id:int}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task AssignRolesAsync(
        int id,
        [FromBody] AssignRolesDto input,
        CancellationToken cancellationToken)
        => userAppService.AssignRolesAsync(id, input.Roles, cancellationToken);

    /// <summary>Activates or deactivates the user.</summary>
    [HttpPut("{id:int}/active-state")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task SetActiveStateAsync(
        int id,
        [FromBody] SetActiveStateDto input,
        CancellationToken cancellationToken)
        => userAppService.SetActiveStateAsync(id, input.IsActive, cancellationToken);
}
