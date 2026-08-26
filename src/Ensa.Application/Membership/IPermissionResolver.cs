using Ensa.Domain.Membership;

namespace Ensa.Application.Membership;

/// <summary>
/// Resolves the effective permission set of a user.
/// <para>
/// Used both by <c>/connect/token</c> (when writing the <c>ensa:permission</c> claims into the
/// token) and by <c>IAccountAppService.GetPermissionsAsync</c>, so that the token contents and the
/// permission list returned by the API come from a <b>single source</b>.
/// </para>
/// </summary>
public interface IPermissionResolver
{
    /// <summary>Returns every permission the user holds, merged from all supported sources.</summary>
    Task<IReadOnlyList<string>> GetPermissionsAsync(User user, CancellationToken cancellationToken = default);
}
