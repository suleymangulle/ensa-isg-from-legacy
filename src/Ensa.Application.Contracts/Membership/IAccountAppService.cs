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

    /// <summary>
    /// The offices the signed-in user may work in, the one to start on, and whether they may take
    /// the "all offices" scope. The shell's office switcher is built from this and nothing else.
    /// <para>
    /// Available to every authenticated user, like <see cref="GetPermissionsAsync"/> — it answers a
    /// question about the caller's own account. It is <b>not</b> the office directory: the office
    /// administration endpoints on <c>api/office</c> keep their <c>Ensa.Office</c> permission, and
    /// this returns only what the caller is already entitled to work in.
    /// </para>
    /// </summary>
    Task<MyOfficesDto> GetMyOfficesAsync(CancellationToken cancellationToken = default);
}
