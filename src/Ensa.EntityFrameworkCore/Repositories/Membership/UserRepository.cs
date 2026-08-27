using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Membership;
using Ensa.Domain.Shared;
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
    /// The account, the person, the contract and the role assignments, answered in one query.
    /// <para>
    /// These five facts used to be five columns on <c>User</c>. They now live in the tables that
    /// own them, and asking four tables at every call site would be four chances to ask the wrong
    /// one — so authorization asks this instead.
    /// </para>
    /// <para>
    /// The joins are left joins on purpose: a user with no profile row or no employment row is a
    /// broken record, not an authorized one, and the defaults below say so — inactive, no type.
    /// </para>
    /// </summary>
    public async Task<UserAuthorizationFacts?> GetAuthorizationFactsAsync(
        int userId,
        CancellationToken ct = default)
    {
        // Both filters are disabled deliberately, and for different reasons.
        //
        // Soft delete: a deleted user must produce facts that SAY deleted, not no facts at all,
        // so the caller can tell "gone" from "never existed".
        //
        // Tenancy: this is the question asked while somebody is signing in, before their tenant
        // is known -- so the ambient tenant is still the host, and a tenant's UserProfile row
        // would be filtered away. The user would then read as inactive and be refused a token,
        // which is exactly how this broke the first time. The lookup is by explicit user id, so
        // the filter protects nothing here.
        using (_dataFilter.Disable<ISoftDelete>())
        using (_dataFilter.Disable<IMultiTenant>())
        {
            var facts = await (
                from user in Context.Set<User>().AsNoTracking()
                where user.Id == userId
                join profileRow in Context.Set<UserProfile>().AsNoTracking()
                    on user.Id equals profileRow.UserId into profiles
                from profile in profiles.DefaultIfEmpty()
                join employmentRow in Context.Set<UserEmployment>().AsNoTracking()
                    on user.Id equals employmentRow.UserId into employments
                from employment in employments.DefaultIfEmpty()
                select new
                {
                    IsActive = profile != null && profile.IsActive,
                    user.IsDeleted,
                    UserTypeId = employment != null ? employment.UserTypeId : null,
                    user.TenantId,
                }).FirstOrDefaultAsync(ct);

            if (facts is null)
            {
                return null;
            }

            var isSystemAdministrator = await (
                from assignment in Context.Set<IdentityUserRole<int>>().AsNoTracking()
                join role in Context.Set<Role>().AsNoTracking() on assignment.RoleId equals role.Id
                where assignment.UserId == userId && role.Name == EnsaRoleNames.SystemAdministrator
                select role.Id).AnyAsync(ct);

            return new UserAuthorizationFacts(
                facts.IsActive,
                facts.IsDeleted,
                isSystemAdministrator,
                facts.UserTypeId,
                facts.TenantId);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// All child collections are filled with a constant number of queries; no separate query is opened
    /// per collection element (no N+1).
    /// </remarks>
    /// <inheritdoc />
    public async Task<Dictionary<int, UserDisplay>> GetDisplaysAsync(
        IEnumerable<int> userIds,
        CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        // Left join, and the tenant filter left alone: a screen asks for the people who appear on
        // the record in front of it, and those are already the people it is allowed to see.
        var rows = await (
            from user in Context.Set<User>().AsNoTracking()
            where ids.Contains(user.Id)
            join profileRow in Context.Set<UserProfile>().AsNoTracking()
                on user.Id equals profileRow.UserId into profiles
            from profile in profiles.DefaultIfEmpty()
            select new
            {
                user.Id,
                user.UserName,
                Name = profile != null ? profile.Name : null,
                LastName = profile != null ? profile.LastName : null,
                IsActive = profile != null && profile.IsActive,
            }).ToListAsync(ct);

        return rows.ToDictionary(
            row => row.Id,
            row =>
            {
                var fullName = $"{row.Name} {row.LastName}".Trim();

                return new UserDisplay(
                    row.Id,
                    string.IsNullOrWhiteSpace(fullName) ? row.UserName ?? string.Empty : fullName,
                    row.UserName,
                    row.IsActive);
            });
    }

    public async Task<UserNavigation?> GetWithNavigationAsync(int id, CancellationToken ct = default)
    {
        var user = await GetReadOnlyQueryable().FirstOrDefaultAsync(k => k.Id == id, ct);
        if (user is null)
        {
            return null;
        }

        var navigation = new UserNavigation { User = user };

        // The person and the contract, which used to be columns on the row above.
        var profile = await Context.Set<UserProfile>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == id, ct);

        var employment = await Context.Set<UserEmployment>().AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == id, ct);

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
        // The assignments are the whole answer now. User.OfficeId used to name one "default"
        // office alongside them, which cannot express a specialist who works in two -- and the
        // legacy data says many do, which is why KullaniciOfis_T holds 1,949 rows.
        var defaultOfficeId = navigation.OfficeAssignments
            .OrderByDescending(ko => ko.MonthlyWorkDurationMinutes)
            .Select(ko => (int?)ko.OfficeId)
            .FirstOrDefault();

        if (officeIds.Count > 0)
        {
            var offices = await Context.Set<Office>()
                .AsNoTracking()
                .Where(o => officeIds.Contains(o.Id))
                .OrderBy(o => o.Name)
                .ToListAsync(ct);

            navigation.Offices = offices;
            navigation.Office = offices.Find(o => o.Id == defaultOfficeId);
        }

        // Roles: a single join query.
        navigation.Roles = await (from assignment in Context.Set<IdentityUserRole<int>>().AsNoTracking()
                                   join role in Context.Set<Role>().AsNoTracking() on assignment.RoleId equals role.Id
                                   where assignment.UserId == id
                                   orderby role.Name
                                   select role)
                                  .ToListAsync(ct);

        // Permissions are deliberately NOT filled here. IPermissionManager is the single
        // implementation of the legacy four-gate rules, and this repository used to carry a
        // second copy of them -- two answers to "what may this user do", free to disagree,
        // and it was the copy feeding the screen that shows an administrator what someone
        // can do. The application service asks the manager.

        // Read through the link rather than by searching UserType for a row whose StaffRole
        // matches the user's: the same fact was stored in both places and free to disagree.
        if (employment?.UserTypeId is int userTypeId)
        {
            navigation.UserType = await Context.Set<UserType>()
                .AsNoTracking()
                .FirstOrDefaultAsync(kt => kt.Id == userTypeId, ct);
        }

        if (profile?.CityId is int cityId)
        {
            navigation.CityName = await Context.Set<City>()
                .AsNoTracking()
                .Where(s => s.Id == cityId)
                .Select(s => (string?)s.CityName)
                .FirstOrDefaultAsync(ct);
        }

        if (profile?.DistrictId is int districtId)
        {
            navigation.DistrictName = await Context.Set<District>()
                .AsNoTracking()
                .Where(i => i.Id == districtId)
                .Select(i => (string?)i.DistrictName)
                .FirstOrDefaultAsync(ct);
        }

        if (profile?.PhotoDocumentId is int photoDocumentId)
        {
            navigation.PhotoDocumentBoyutu = await Context.Set<Document>()
                .AsNoTracking()
                .Where(d => d.Id == photoDocumentId)
                .Select(d => (long?)d.SizeBytes)
                .FirstOrDefaultAsync(ct);
        }

        // ASSUMPTION: there is no separate mapping table for multi-organization access.
        // A system administrator reaches every active organization, other accounts only their own.
        // Identity owns roles, so this asks the role assignment rather than a boolean the user
        // row used to carry beside it.
        var isSystemAdministrator = await (
            from assignment in Context.Set<IdentityUserRole<int>>().AsNoTracking()
            join role in Context.Set<Role>().AsNoTracking() on assignment.RoleId equals role.Id
            where assignment.UserId == id && role.Name == EnsaRoleNames.SystemAdministrator
            select role.Id).AnyAsync(ct);

        if (isSystemAdministrator)
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
}
