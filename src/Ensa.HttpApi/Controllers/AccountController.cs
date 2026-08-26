using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Membership;
using Ensa.Application.Contracts.Membership.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Self-service account endpoints — <c>api/account</c>.
/// <para>
/// Everything here acts on the <b>signed-in user's own</b> account, so none of it carries a
/// permission policy: only the inherited <c>[Authorize]</c>, which requires a valid token. A
/// permission check would be wrong rather than merely strict — a user with no permissions at all
/// still has to be able to read their own profile and change their own password. The user id is
/// taken from the validated token, never from the request, so one user cannot act on another's
/// account through these routes.
/// </para>
/// <para>
/// Administrative operations on <i>other</i> users — creating them, resetting their password,
/// assigning roles — live on <c>api/user</c> and are permission-guarded there.
/// </para>
/// <para>
/// Tokens are not issued here but by OpenIddict at <c>/connect/token</c>. This controller covers
/// what happens <b>after</b> a token has been obtained.
/// </para>
/// </summary>
public class AccountController(IAccountAppService accountAppService) : EnsaController
{
    /// <summary>The signed-in user's own profile.</summary>
    [HttpGet("profile")]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ProfileDto> GetProfileAsync()
        => accountAppService.GetProfileAsync();

    /// <summary>
    /// Changes the signed-in user's own password.
    /// <para>
    /// The current password is required, so a stolen access token alone is not enough to take
    /// over an account. The seeded administrator is flagged to change its password on first
    /// sign-in and this is the endpoint that clears the flag.
    /// </para>
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordDto input)
    {
        await accountAppService.ChangePasswordAsync(input);
        return NoContent();
    }

    /// <summary>
    /// The permission names the signed-in user holds.
    /// <para>
    /// The SPA drives menu and button visibility from this list. It is a convenience for the
    /// interface only — every endpoint still enforces its own permission server-side, so hiding
    /// a button is never the control that protects an operation.
    /// </para>
    /// </summary>
    [HttpGet("permissions")]
    [ProducesResponseType<ListResultDto<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ListResultDto<string>> GetPermissionsAsync()
        => accountAppService.GetPermissionsAsync();
}
