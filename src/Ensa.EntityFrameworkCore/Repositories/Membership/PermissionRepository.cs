using Ensa.Domain.Common;
using Ensa.Domain.Membership;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Membership;

/// <summary>
/// Queries specific to the <see cref="Permission"/> module.
/// <para>
/// <see cref="Permission"/>, <see cref="UserTypePermission"/>, <see cref="OrganizationTypePermission"/> and
/// <see cref="SubscriptionPlanPermission"/> are host catalogue tables (they do not implement
/// <c>IMultiTenant</c>). <see cref="UserPermission"/> and <see cref="PermissionRestriction"/>, by contrast,
/// belong to a tenant; the global query filter narrows them to the current tenant, so no <c>TenantId</c>
/// predicate is written in these queries.
/// </para>
/// </summary>
public class PermissionRepository(EnsaDbContext context, IDataFilter dataFilter)
    : EfCoreRepository<Permission>(context, dataFilter), IPermissionRepository
{
    /// <inheritdoc />
    public Task<Permission?> FindByTargetAsync(string permissionTarget, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(permissionTarget))
        {
            return Task.FromResult<Permission?>(null);
        }

        return GetReadOnlyQueryable().FirstOrDefaultAsync(y => y.PermissionTarget == permissionTarget, ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The id set is translated into a single <c>IN (...)</c> query; for an empty set the database is
    /// never hit.
    /// </remarks>
    public Task<List<Permission>> GetByIdsAsync(IEnumerable<int> permissionIds, CancellationToken ct = default)
    {
        var ids = NormalizeIds(permissionIds);
        if (ids.Count == 0)
        {
            return Task.FromResult(new List<Permission>());
        }

        return GetReadOnlyQueryable()
            .Where(y => ids.Contains(y.Id))
            .OrderBy(y => y.SortOrder)
            .ThenBy(y => y.Id)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public Task<List<int>> GetUserRolePermissionIdsAsync(int userRoleId, CancellationToken ct = default)
        => Context.Set<UserTypePermission>()
                  .AsNoTracking()
                  .Where(x => x.UserTypeId == userRoleId && x.IsActive)
                  .Select(x => x.PermissionId)
                  .Distinct()
                  .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<int>> GetUserPermissionPermissionIdsAsync(int userId, CancellationToken ct = default)
        => Context.Set<UserPermission>()
                  .AsNoTracking()
                  .Where(x => x.UserId == userId && x.Authorized && x.IsActive)
                  .Select(x => x.PermissionId)
                  .Distinct()
                  .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<int>> GetUserRedPermissionIdsAsync(int userId, CancellationToken ct = default)
        => Context.Set<UserPermission>()
                  .AsNoTracking()
                  .Where(x => x.UserId == userId && !x.Authorized && x.IsActive)
                  .Select(x => x.PermissionId)
                  .Distinct()
                  .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<int>> GetOrganizationTypePermissionIdsAsync(int organizationTypeId, CancellationToken ct = default)
        => Context.Set<OrganizationTypePermission>()
                  .AsNoTracking()
                  .Where(x => x.OrganizationTypeId == organizationTypeId)
                  .Select(x => x.PermissionId)
                  .Distinct()
                  .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<int>> GetSubscriptionPlanPermissionIdsAsync(int subscriptionPlanId, CancellationToken ct = default)
        => Context.Set<SubscriptionPlanPermission>()
                  .AsNoTracking()
                  .Where(x => x.SubscriptionPlanId == subscriptionPlanId)
                  .Select(x => x.PermissionId)
                  .Distinct()
                  .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<int>> GetPermissionRestrictionUserRoleIdsAsync(int permissionId, CancellationToken ct = default)
        => Context.Set<PermissionRestriction>()
                  .AsNoTracking()
                  .Where(x => x.PermissionId == permissionId)
                  .Select(x => x.UserTypeId)
                  .Distinct()
                  .ToListAsync(ct);

    /// <summary>
    /// Returns the restriction lists of the given permissions <b>in a SINGLE query</b>.
    /// <para>
    /// Instead of a query per permission (N+1), all rows are fetched at once with
    /// <c>WHERE PermissionId IN (...)</c> and grouped in memory. Permissions with no restriction rows do not
    /// appear in the dictionary — the caller interprets that as "unrestricted".
    /// </para>
    /// </summary>
    public async Task<Dictionary<int, List<int>>> GetPermissionRestrictionMapAsync(
        IEnumerable<int> permissionIds,
        CancellationToken ct = default)
    {
        var ids = NormalizeIds(permissionIds);
        if (ids.Count == 0)
        {
            return [];
        }

        var lines = await Context.Set<PermissionRestriction>()
            .AsNoTracking()
            .Where(x => ids.Contains(x.PermissionId))
            .Select(x => new { x.PermissionId, x.UserTypeId })
            .ToListAsync(ct);

        return lines
            .GroupBy(x => x.PermissionId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.UserTypeId).Distinct().ToList());
    }

    /// <inheritdoc />
    public Task<List<int>> GetLinkTargetIdsAsync(
        int permissionId,
        PermissionScopeType linkType,
        CancellationToken ct = default)
        => Context.Set<PermissionScope>()
                  .AsNoTracking()
                  .Where(x => x.PermissionId == permissionId
                              && x.LinkType == linkType
                              && x.IsActive
                              && x.LinkTargetId != null)
                  .Select(x => x.LinkTargetId!.Value)
                  .Distinct()
                  .ToListAsync(ct);

    /// <summary>Deduplicates an id set and turns it into a list (used as the <c>IN</c> parameter).</summary>
    private static List<int> NormalizeIds(IEnumerable<int>? ids)
        => ids is null ? [] : [.. ids.Distinct()];
}
