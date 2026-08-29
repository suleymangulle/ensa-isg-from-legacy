using Ensa.Domain.Repositories;

namespace Ensa.Domain.Membership;

/// <summary>
/// Module-specific repository contract for <see cref="Permission"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface IPermissionRepository : IRepository<Permission>
{
    /// <summary>Finds a permission by its protected target name (an <c>EnsaPermissions</c> constant).</summary>
    Task<Permission?> FindByTargetAsync(string permissionTarget, CancellationToken ct = default);

    /// <summary>Loads the permissions in the given id set.</summary>
    Task<List<Permission>> GetByIdsAsync(IEnumerable<int> permissionIds, CancellationToken ct = default);

    /// <summary>Ids of the active permissions granted by default to a user type.</summary>
    Task<List<int>> GetUserRolePermissionIdsAsync(int userRoleId, CancellationToken ct = default);

    /// <summary>Ids of the permissions explicitly GRANTED to a user (<c>IsAuthorized == true</c>, active).</summary>
    Task<List<int>> GetUserPermissionPermissionIdsAsync(int userId, CancellationToken ct = default);

    /// <summary>Ids of the permissions explicitly DENIED to a user (<c>IsAuthorized == false</c>, active).</summary>
    Task<List<int>> GetUserRedPermissionIdsAsync(int userId, CancellationToken ct = default);

    /// <summary>Ids of the permissions opened up to an organization type (mandatory gate).</summary>
    Task<List<int>> GetOrganizationTypePermissionIdsAsync(int organizationTypeId, CancellationToken ct = default);

    /// <summary>Ids of the permissions included in a subscription plan (mandatory gate).</summary>
    Task<List<int>> GetSubscriptionPlanPermissionIdsAsync(int subscriptionPlanId, CancellationToken ct = default);

    /// <summary>User type ids on a permission's restriction list.</summary>
    Task<List<int>> GetPermissionRestrictionUserRoleIdsAsync(int permissionId, CancellationToken ct = default);

    /// <summary>
    /// Returns the restriction lists of the given permissions in a SINGLE query:
    /// <c>PermissionId → list of restricted UserTypeId</c>.
    /// Used to avoid N+1 queries while computing effective permissions.
    /// Permissions with no restriction rows are ABSENT from the dictionary.
    /// </summary>
    Task<Dictionary<int, List<int>>> GetPermissionRestrictionMapAsync(
        IEnumerable<int> permissionIds,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the active target ids of the given link type among the <see cref="PermissionScope"/>
    /// records attached to a permission (used when computing menu/module visibility).
    /// </summary>
    Task<List<int>> GetLinkTargetIdsAsync(
        int permissionId,
        Shared.Enums.PermissionScopeType linkType,
        CancellationToken ct = default);
}
