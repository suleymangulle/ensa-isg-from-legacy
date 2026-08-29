using Ensa.Domain.Common;
using Ensa.Domain.Membership;
using Ensa.Domain.Lookups;
using Ensa.Domain.Tenancy;
using Ensa.Domain.Tenancy.Navigations;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Tenancy;

/// <summary>
/// Queries specific to the <see cref="Office"/> module.
/// <para>
/// <see cref="Office"/> is tenant filtered; every query runs in the current tenant context and the
/// <c>TenantId</c> predicate is never written by hand.
/// </para>
/// </summary>
public class OfficeRepository(EnsaDbContext context, IDataFilter dataFilter)
    : EfCoreRepository<Office>(context, dataFilter), IOfficeRepository
{
    /// <inheritdoc />
    public Task<Office?> FindHeadquarterOfficeAsync(CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .Where(o => o.HeadquarterOffice && o.IsActive)
            .OrderBy(o => o.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// The user count comes both from the default office field (<c>User.OfficeId</c>) and from the
    /// multi-assignment table (<see cref="UserOffice"/>); the two are combined with a subquery in a
    /// SINGLE query (the same user is never counted twice).
    /// </remarks>
    public async Task<OfficeNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var office = await GetReadOnlyQueryable().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (office is null)
        {
            return null;
        }

        var navigation = new OfficeNavigation { Office = office };

        if (office.TenantId is int organizationId)
        {
            // Organization is a host table; no tenant filter is applied.
            navigation.Organization = await Context.Set<Organization>()
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == organizationId, cancellationToken);
        }

        if (office.CityId is int cityId)
        {
            navigation.CityName = await Context.Set<City>()
                .AsNoTracking()
                .Where(s => s.Id == cityId)
                .Select(s => (string?)s.CityName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (office.DistrictId is int districtId)
        {
            navigation.DistrictName = await Context.Set<District>()
                .AsNoTracking()
                .Where(i => i.Id == districtId)
                .Select(i => (string?)i.DistrictName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var assignedUserIds = Context.Set<UserOffice>()
            .AsNoTracking()
            .Where(ko => ko.OfficeId == id)
            .Select(ko => ko.UserId);

        // Every office a user works in is an assignment now -- the per-user "default office"
        // column was folded into UserOffice, so the assignments are the whole answer.
        navigation.UserCount = await Context.Set<UserProfile>()
            .AsNoTracking()
            .CountAsync(
                p => p.IsActive && assignedUserIds.Contains(p.UserId),
                cancellationToken);

        return navigation;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The assignment table is embedded as an <c>IN (...)</c> subquery: one round trip and no risk of
    /// duplicate rows (even when the assignments reference the same office more than once).
    /// </remarks>
    public Task<List<Office>> GetUserOfficesAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var assignedOfficeIds = Context.Set<UserOffice>()
            .AsNoTracking()
            .Where(ko => ko.UserId == userId)
            .Select(ko => ko.OfficeId);

        return GetReadOnlyQueryable()
            .Where(o => assignedOfficeIds.Contains(o.Id) && o.IsActive)
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int?> FindDefaultUserOfficeIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Ordered by the assignment row's own id, not the office's: it is the order the rows were
        // written in that carries the meaning, and the migration wrote the legacy default first.
        var officeIds = await Context.Set<UserOffice>()
            .AsNoTracking()
            .Where(uo => uo.UserId == userId)
            .OrderBy(uo => uo.Id)
            .Select(uo => (int?)uo.OfficeId)
            .Take(1)
            .ToListAsync(cancellationToken);

        return officeIds.Count > 0 ? officeIds[0] : null;
    }

    /// <inheritdoc />
    public Task<List<Office>> GetByCompanyAsync(int companyId, CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .Where(o => o.CompanyId == companyId)
            .OrderByDescending(o => o.HeadquarterOffice)
            .ThenBy(o => o.Name)
            .ToListAsync(cancellationToken);
}
