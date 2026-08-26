using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Membership.Dtos;

namespace Ensa.Application.Contracts.Membership;

/// <summary>
/// Operations the signed-in user performs on their own account.
/// <para>
/// Tokens are not issued here but through <c>/connect/token</c> (OpenIddict). This service covers
/// the account operations that happen <b>after</b> a token has been obtained.
/// </para>
/// </summary>
public interface IAccountAppService : IApplicationService
{
    /// <summary>Returns the profile of the signed-in user.</summary>
    Task<ProfileDto> GetProfileAsync();

    /// <summary>Changes the password of the signed-in user.</summary>
    Task ChangePasswordAsync(ChangePasswordDto input);

    /// <summary>
    /// Returns the permission names held by the signed-in user.
    /// The frontend drives menu and button visibility from this list.
    /// </summary>
    Task<ListResultDto<string>> GetPermissionsAsync();
}
