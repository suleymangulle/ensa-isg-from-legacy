using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Membership;
using Ensa.Domain.Membership.Navigations;
using Ensa.Domain.Lookups;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Membership;

/// <summary>
/// Queries specific to the <see cref="User"/> module.
/// <para>
/// <see cref="User"/> implements both <c>IMultiTenant</c> and <c>ISoftDelete</c>; unless stated otherwise
/// every query goes through the global query filters and the <c>TenantId</c> / <c>IsDeleted</c> predicate is
/// never written by hand.
/// </para>
/// </summary>
public class UserRepository(
    EnsaDbContext context,
    IDataFilter dataFilter,
    ICurrentTenant currentTenant)
    : EfCoreRepository<User>(context, dataFilter), IUserRepository
{
    private readonly IDataFilter _dataFilter = dataFilter;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// Computes the user's EFFECTIVE permissions (see <see cref="IPermissionManager"/> for the rule order).
    /// <para>
    /// <b>Use of IDataFilter — why it is needed:</b> this method is called mostly during <b>token/claim
    /// generation</b>, at which point <c>ICurrentTenant</c> has not been established yet (the tenant is
    /// about to be read from this very user record). With the tenant global filter on, the user record
    /// cannot be found and everybody looks unauthorised. The filter is therefore disabled ONLY while the
    /// user record is read.
    /// </para>
    /// <para>
    /// <b>Leak prevention:</b> right after the user is found, <see cref="ICurrentTenant.Change"/> switches
    /// to the user's own tenant, so that the tenant-owned child tables (<see cref="UserPermission"/>,
    /// <see cref="PermissionRestriction"/>) are read under the global filter again and with the CORRECT
    /// tenant. The filter is never left disabled.
    /// </para>
    /// </summary>
    public async Task<List<Permission>> GetPermissionsAsync(int userId, CancellationToken ct = default)
    {
        User? user;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            user = await GetReadOnlyQueryable()
                .FirstOrDefaultAsync(k => k.Id == userId, ct);
        }

        if (user is null || !user.IsActive)
        {
            return [];
        }

        using (_currentTenant.Change(user.TenantId))
        {
            // 1) System administrator: all permissions, no gate checks.
            if (user.SystemAdministrator)
            {
                return await AllPermissionsAsync(ct);
            }

            if (user.TenantId is not int organizationId)
            {
                // A host user without the system administrator role has no permissions.
                return [];
            }

            // Organization is a host table; no tenant filter is applied.
            var organization = await Context.Set<Organization>()
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == organizationId, ct);

            if (organization is null || !organization.IsActive)
            {
                return [];
            }

            // 2) Subscription plan gate
            var packageIds = await Context.Set<SubscriptionPlanPermission>()
                .AsNoTracking()
                .Where(x => x.SubscriptionPlanId == organization.SubscriptionPlanId)
                .Select(x => x.PermissionId)
                .ToListAsync(ct);

            if (packageIds.Count == 0)
            {
                return [];
            }

            // 3) Organization type gate
            var organizationTypeIds = await Context.Set<OrganizationTypePermission>()
                .AsNoTracking()
                .Where(x => x.OrganizationTypeId == organization.OrganizationTypeId)
                .Select(x => x.PermissionId)
                .ToListAsync(ct);

            if (organizationTypeIds.Count == 0)
            {
                return [];
            }

            var userRoleId = await FindUserRoleIdAsync(user.StaffRole, ct);

            // 4) Source union: user type defaults + permissions granted to the user
            var sourceIds = new HashSet<int>();

            if (userRoleId is int typeId)
            {
                var typePermissions = await Context.Set<UserTypePermission>()
                    .AsNoTracking()
                    .Where(x => x.UserTypeId == typeId && x.IsActive)
                    .Select(x => x.PermissionId)
                    .ToListAsync(ct);

                sourceIds.UnionWith(typePermissions);
            }

            // The user's grant/deny rows are read in a SINGLE query (instead of two round trips).
            var userLines = await Context.Set<UserPermission>()
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.IsActive)
                .Select(x => new { x.PermissionId, x.Authorized })
                .ToListAsync(ct);

            sourceIds.UnionWith(userLines.Where(x => x.Authorized).Select(x => x.PermissionId));

            if (sourceIds.Count == 0)
            {
                return [];
            }

            // 5) An explicit denial overrides everything.
            sourceIds.ExceptWith(userLines.Where(x => !x.Authorized).Select(x => x.PermissionId));

            // Rows that made it through the gates
            sourceIds.IntersectWith(packageIds);
            sourceIds.IntersectWith(organizationTypeIds);

            if (sourceIds.Count == 0)
            {
                return [];
            }

            var candidateIds = sourceIds.ToList();

            var permissions = await Context.Set<Permission>()
                .AsNoTracking()
                .Where(y => candidateIds.Contains(y.Id))
                .OrderBy(y => y.SortOrder)
                .ThenBy(y => y.Id)
                .ToListAsync(ct);

            // 6) User type restriction
            if (userRoleId is not int activeTypeId)
            {
                // When the user type is undefined, only unrestricted (Everyone) permissions apply.
                return permissions.Where(y => y.PermissionRestrictionMode == PermissionRestrictionMode.Everyone).ToList();
            }

            var restrictedIds = permissions
                .Where(y => y.PermissionRestrictionMode != PermissionRestrictionMode.Everyone)
                .Select(y => y.Id)
                .ToList();

            if (restrictedIds.Count == 0)
            {
                return permissions;
            }

            // Restriction rows are read in a SINGLE query — no per-permission query (N+1).
            var restrictionLines = await Context.Set<PermissionRestriction>()
                .AsNoTracking()
                .Where(x => restrictedIds.Contains(x.PermissionId) && x.UserTypeId == activeTypeId)
                .Select(x => x.PermissionId)
                .ToListAsync(ct);

            var listedIds = restrictionLines.ToHashSet();

            return permissions
                .Where(y => y.PermissionRestrictionMode switch
                {
                    PermissionRestrictionMode.OnlySelected => listedIds.Contains(y.Id),
                    PermissionRestrictionMode.SelectedExcept => !listedIds.Contains(y.Id),
                    _ => true
                })
                .ToList();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// All child collections are filled with a constant number of queries; no separate query is opened
    /// per collection element (no N+1).
    /// </remarks>
    public async Task<UserNavigation?> GetWithNavigationAsync(int id, CancellationToken ct = default)
    {
        var user = await GetReadOnlyQueryable().FirstOrDefaultAsync(k => k.Id == id, ct);
        if (user is null)
        {
            return null;
        }

        var navigation = new UserNavigation { User = user };

        if (user.TenantId is int organizationId)
        {
            navigation.Organization = await Context.Set<Organization>()
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == organizationId, ct);
        }

        // Office assignments and offices: two queries, no query per element.
        navigation.OfficeAssignments = await Context.Set<UserOffice>()
            .AsNoTracking()
            .Where(ko => ko.UserId == id)
            .ToListAsync(ct);

        var officeIds = navigation.OfficeAssignments.Select(ko => ko.OfficeId).ToList();
        if (user.OfficeId is int defaultOfficeId && !officeIds.Contains(defaultOfficeId))
        {
            officeIds.Add(defaultOfficeId);
        }

        if (officeIds.Count > 0)
        {
            var offices = await Context.Set<Office>()
                .AsNoTracking()
                .Where(o => officeIds.Contains(o.Id))
                .OrderBy(o => o.Name)
                .ToListAsync(ct);

            navigation.Offices = offices;
            navigation.Office = offices.Find(o => o.Id == user.OfficeId);
        }

        // Roles: a single join query.
        navigation.Roles = await (from assignment in Context.Set<IdentityUserRole<int>>().AsNoTracking()
                                   join role in Context.Set<Role>().AsNoTracking() on assignment.RoleId equals role.Id
                                   where assignment.UserId == id
                                   orderby role.Name
                                   select role)
                                  .ToListAsync(ct);

        navigation.Permissions = await GetPermissionsAsync(id, ct);

        navigation.UserType = await Context.Set<UserType>()
            .AsNoTracking()
            .Where(kt => kt.StaffRole == user.StaffRole && kt.IsActive)
            .OrderBy(kt => kt.SortOrder)
            .FirstOrDefaultAsync(ct);

        if (user.CityId is int cityId)
        {
            navigation.CityName = await Context.Set<City>()
                .AsNoTracking()
                .Where(s => s.Id == cityId)
                .Select(s => (string?)s.CityName)
                .FirstOrDefaultAsync(ct);
        }

        if (user.DistrictId is int districtId)
        {
            navigation.DistrictName = await Context.Set<District>()
                .AsNoTracking()
                .Where(i => i.Id == districtId)
                .Select(i => (string?)i.DistrictName)
                .FirstOrDefaultAsync(ct);
        }

        if (user.PhotoDocumentId is int photoDocumentId)
        {
            navigation.PhotoDocumentBoyutu = await Context.Set<Document>()
                .AsNoTracking()
                .Where(d => d.Id == photoDocumentId)
                .Select(d => (long?)d.SizeBytes)
                .FirstOrDefaultAsync(ct);
        }

        // ASSUMPTION: there is no separate mapping table for multi-organization access.
        // A system administrator reaches every active organization, other accounts only their own.
        if (user.SystemAdministrator)
        {
            navigation.OrganizationIds = await Context.Set<Organization>()
                .AsNoTracking()
                .Where(k => k.IsActive)
                .Select(k => k.Id)
                .ToListAsync(ct);
        }
        else if (user.TenantId is int tenantId)
        {
            navigation.OrganizationIds = [tenantId];
        }

        return navigation;
    }

    /// <inheritdoc />
    public Task<bool> NationalIdExistsAsync(
        string nationalId,
        int? exceptUserId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
        {
            return Task.FromResult(false);
        }

        return GetReadOnlyQueryable()
            .AnyAsync(
                k => k.NationalId == nationalId
                     && (exceptUserId == null || k.Id != exceptUserId),
                ct);
    }

    /// <summary>
    /// Searches by normalised user name (the tenant filter IS applied).
    /// <para>
    /// The comparison runs on <c>NormalizedUserName</c>, so the result matches ASP.NET Core Identity
    /// regardless of the database collation.
    /// </para>
    /// </summary>
    public Task<User?> FindByUserNameAsync(string userName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return Task.FromResult<User?>(null);
        }

        var normalize = userName.Trim().ToUpperInvariant();

        return GetReadOnlyQueryable()
            .FirstOrDefaultAsync(k => k.NormalizedUserName == normalize, ct);
    }

    /// <inheritdoc />
    /// <remarks>The assignment table is embedded as an <c>IN (...)</c> subquery — a single round trip.</remarks>
    public Task<List<User>> GetByOfficeAsync(int officeId, CancellationToken ct = default)
    {
        var assignedIds = Context.Set<UserOffice>()
            .AsNoTracking()
            .Where(ko => ko.OfficeId == officeId)
            .Select(ko => ko.UserId);

        return GetReadOnlyQueryable()
            .Where(k => k.IsActive && (k.OfficeId == officeId || assignedIds.Contains(k.Id)))
            .OrderBy(k => k.Name)
            .ThenBy(k => k.LastName)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Role names are matched on <c>NormalizedName</c> and all roles are resolved in a SINGLE query
    /// (no query per role).
    /// </remarks>
    public Task<List<User>> GetByRolesAsync(IEnumerable<string> roleNames, CancellationToken ct = default)
    {
        var normalizedNames = (roleNames ?? [])
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (normalizedNames.Count == 0)
        {
            return Task.FromResult(new List<User>());
        }

        var roleIds = Context.Set<Role>()
            .AsNoTracking()
            .Where(r => r.NormalizedName != null && normalizedNames.Contains(r.NormalizedName))
            .Select(r => r.Id);

        var userIds = Context.Set<IdentityUserRole<int>>()
            .AsNoTracking()
            .Where(ur => roleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId);

        return GetReadOnlyQueryable()
            .Where(k => k.IsActive && userIds.Contains(k.Id))
            .OrderBy(k => k.Name)
            .ThenBy(k => k.LastName)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public Task<List<string>> GetRoleNamesAsync(int userId, CancellationToken ct = default)
        => (from assignment in Context.Set<IdentityUserRole<int>>().AsNoTracking()
            join role in Context.Set<Role>().AsNoTracking() on assignment.RoleId equals role.Id
            where assignment.UserId == userId && role.Name != null
            select role.Name!)
           .Distinct()
           .ToListAsync(ct);

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>Returns the whole permission catalogue (system administrator shortcut).</summary>
    private Task<List<Permission>> AllPermissionsAsync(CancellationToken ct)
        => Context.Set<Permission>()
                  .AsNoTracking()
                  .OrderBy(y => y.SortOrder)
                  .ThenBy(y => y.Id)
                  .ToListAsync(ct);

    /// <summary>
    /// Resolves the id of the <see cref="UserType"/> record for a <see cref="StaffRole"/> enum value.
    /// (In the legacy system this match was done with string comparison.)
    /// </summary>
    private async Task<int?> FindUserRoleIdAsync(StaffRole staffRole, CancellationToken ct)
    {
        if (staffRole == StaffRole.Unspecified)
        {
            return null;
        }

        return await Context.Set<UserType>()
            .AsNoTracking()
            .Where(kt => kt.StaffRole == staffRole && kt.IsActive)
            .OrderBy(kt => kt.SortOrder)
            .Select(kt => (int?)kt.Id)
            .FirstOrDefaultAsync(ct);
    }
}
