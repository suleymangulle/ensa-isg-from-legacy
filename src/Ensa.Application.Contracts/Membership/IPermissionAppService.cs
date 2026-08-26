using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Membership.Dtos;
using Ensa.Application.Contracts.Membership.Dtos.Navigations;

namespace Ensa.Application.Contracts.Membership;

/// <summary>
/// Read access to the permission catalogue plus per-user permission assignment.
/// <para>
/// <c>Permission</c> is a host catalogue table seeded from the <c>EnsaPermissions</c>
/// constants, so it is deliberately read-only over HTTP: there is no create or delete
/// endpoint. Adding a permission means adding a constant and re-running the seeder,
/// which keeps the catalogue and the authorization policies from drifting apart.
/// </para>
/// </summary>
public interface IPermissionAppService : IApplicationService
{
    Task<PagedResultDto<PermissionDto>> GetListAsync(
        GetPermissionListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>The whole catalogue as a hierarchy over <c>ParentPermissionId</c>.</summary>
    Task<PermissionTreeDto> GetTreeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Effective permissions of one user plus the explicit grant/deny overrides behind them.
    /// The effective set is computed by <c>IPermissionManager</c>, never here.
    /// </summary>
    Task<UserPermissionsDto> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the explicit grant/deny overrides of one user.</summary>
    Task SaveUserPermissionsAsync(
        int userId,
        UpdateUserPermissionsDto input,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Permission defaults of one staff type — what every physician, or every safety
    /// specialist, can do without a per-user grant.
    /// </summary>
    Task<UserTypePermissionsDto> GetUserTypePermissionsAsync(
        int userTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces the permission defaults of a staff type.</summary>
    Task SaveUserTypePermissionsAsync(
        int userTypeId,
        UpdateUserTypePermissionsDto input,
        CancellationToken cancellationToken = default);
}
