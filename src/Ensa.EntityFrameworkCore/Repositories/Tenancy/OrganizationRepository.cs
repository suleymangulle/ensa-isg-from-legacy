using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Membership;
using Ensa.Domain.Lookups;
using Ensa.Domain.Tenancy;
using Ensa.Domain.Tenancy.Navigations;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Tenancy;

/// <summary>
/// Queries specific to the <see cref="Organization"/> (tenant) module.
/// <para>
/// <see cref="Organization"/> is a <b>host</b> table (it does not implement <c>IMultiTenant</c>); only the
/// soft-delete global filter applies to it. The <see cref="Office"/>, <see cref="User"/> and
/// <see cref="Company"/> records attached to an organization, by contrast, are tenant filtered — see
/// <see cref="GetWithNavigationAsync"/>.
/// </para>
/// </summary>
public class OrganizationRepository(EnsaDbContext context, IDataFilter dataFilter)
    : EfCoreRepository<Organization>(context, dataFilter), IOrganizationRepository
{
    private readonly IDataFilter _dataFilter = dataFilter;

    /// <inheritdoc />
    public Task<Organization?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult<Organization?>(null);
        }

        return GetReadOnlyQueryable().FirstOrDefaultAsync(k => k.Code == code, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> CodeExistsAsync(
        string code,
        int? exceptOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult(false);
        }

        return GetReadOnlyQueryable()
            .AnyAsync(k => k.Code == code && (exceptOrganizationId == null || k.Id != exceptOrganizationId), cancellationToken);
    }

    /// <summary>
    /// Collects the organization together with its type, plan, office and contract details.
    /// <para>
    /// <b>Use of IDataFilter:</b> this method is called from the host administration screens, usually while
    /// <c>CurrentTenant == null</c>, in order to display the profile of <i>another</i> organization. With the
    /// tenant global filter on, the <see cref="Office"/> / <see cref="User"/> / <see cref="Company"/> queries
    /// cannot see that organization's rows and the counters silently return zero. The filter is therefore
    /// disabled; to prevent data leaks every subquery is <b>explicitly</b> narrowed with
    /// <c>TenantId == id</c>.
    /// </para>
    /// </summary>
    public async Task<OrganizationNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var organization = await GetReadOnlyQueryable().FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
        if (organization is null)
        {
            return null;
        }

        var navigation = new OrganizationNavigation { Organization = organization };

        navigation.OrganizationType = await Context.Set<OrganizationType>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == organization.OrganizationTypeId, cancellationToken);

        navigation.SubscriptionPlan = await Context.Set<SubscriptionPlan>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == organization.SubscriptionPlanId, cancellationToken);

        if (organization.CityId is int cityId)
        {
            navigation.CityName = await Context.Set<City>()
                .AsNoTracking()
                .Where(s => s.Id == cityId)
                .Select(s => (string?)s.CityName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (organization.DistrictId is int districtId)
        {
            navigation.DistrictName = await Context.Set<District>()
                .AsNoTracking()
                .Where(i => i.Id == districtId)
                .Select(i => (string?)i.DistrictName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        navigation.CurrentContract = await Context.Set<OrganizationContract>()
            .AsNoTracking()
            .Where(s => s.OrganizationId == id && s.IsActive)
            .OrderByDescending(s => s.ContractDate)
            .ThenByDescending(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        using (_dataFilter.Disable<IMultiTenant>())
        {
            navigation.Offices = await Context.Set<Office>()
                .AsNoTracking()
                .Where(o => o.TenantId == id && o.IsActive)
                .OrderByDescending(o => o.IsHeadquarterOffice)
                .ThenBy(o => o.Name)
                .ToListAsync(cancellationToken);

            navigation.HeadquarterOffice = navigation.Offices.Find(o => o.IsHeadquarterOffice);

            navigation.ActiveUserCount = await Context.Set<UserProfile>()
                .AsNoTracking()
                .CountAsync(p => p.TenantId == id && p.IsActive, cancellationToken);

            navigation.ActiveCompanyCount = await Context.Set<Company>()
                .AsNoTracking()
                .CountAsync(f => f.TenantId == id && f.IsActive && !f.IsOrganizationRecord, cancellationToken);
        }

        return navigation;
    }

    /// <summary>
    /// Number of active (not deleted) users in the organization, for subscription quota checks.
    /// <para>
    /// <b>Use of IDataFilter:</b> <paramref name="organizationId"/> may differ from the current tenant
    /// (host administration / quota report). The tenant filter is disabled and the count is explicitly
    /// narrowed with <c>TenantId == organizationId</c>. The soft-delete filter is left ON: deleted users
    /// must not consume the quota.
    /// </para>
    /// </summary>
    public async Task<int> GetActiveUserCountAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        using (_dataFilter.Disable<IMultiTenant>())
        {
            return await Context.Set<UserProfile>()
                .AsNoTracking()
                .CountAsync(p => p.TenantId == organizationId && p.IsActive, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task<List<Organization>> GetDurationExpiredAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .Where(k => k.IsActive && k.SubscriptionEnd != null && k.SubscriptionEnd < date)
            .OrderBy(k => k.SubscriptionEnd)
            .ToListAsync(cancellationToken);
}
