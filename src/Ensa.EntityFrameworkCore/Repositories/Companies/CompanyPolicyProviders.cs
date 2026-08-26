using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Lookups;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Companies;

/// <summary>
/// <see cref="ITenantLimitProvider"/> implementation — reads the limits from the <c>Organization</c> record.
/// <para>
/// <b>The tenant filter is disabled:</b> this query may run against the <c>Organization</c> table
/// <b>for another tenant</b> (a host administration screen adding a company on behalf of an organization).
/// Besides, the <c>Organization</c> entity does not implement <c>IMultiTenant</c> — it <i>defines</i> the
/// tenant rather than belonging to one. The scope is still kept narrow so that no other filter in the chain
/// is affected, and the <c>Id == tenantId</c> predicate is written <b>explicitly</b>.
/// </para>
/// </summary>
public class TenantLimitProvider(EnsaDbContext context, IDataFilter dataFilter)
    : ITenantLimitProvider
{
    /// <inheritdoc />
    public Task<int?> GetCompanyLimitAsync(int? tenantId, CancellationToken cancellationToken = default)
        => LimitReadAsync(tenantId, k => k.MaximumCompanyCount, cancellationToken);

    /// <inheritdoc />
    public Task<int?> GetCompanyPerUserLimitAsync(
        int? tenantId,
        CancellationToken cancellationToken = default)
        => LimitReadAsync(tenantId, k => k.MaximumUserCount, cancellationToken);

    private async Task<int?> LimitReadAsync(
        int? tenantId,
        Func<Organization, int?> secici,
        CancellationToken cancellationToken)
    {
        // No limit is applied in a host context (no tenant).
        if (tenantId is null)
        {
            return null;
        }

        using (dataFilter.Disable<IMultiTenant>())
        {
            var organization = await context.Set<Organization>()
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == tenantId.Value, cancellationToken);

            return organization is null ? null : secici(organization);
        }
    }
}

/// <summary>
/// <see cref="INaceHazardClassProvider"/> implementation.
/// <para>
/// <c>OccupationCode</c> is a host reference table (it does not implement <c>IMultiTenant</c>), so there is
/// no need to disable the tenant filter.
/// </para>
/// </summary>
public class NaceHazardClassProvider(EnsaDbContext context) : INaceHazardClassProvider
{
    /// <inheritdoc />
    public async Task<HazardClass?> GetHazardClassAsync(
        int occupationCodeId,
        CancellationToken cancellationToken = default)
    {
        var hazardClass = await context.Set<OccupationCode>()
            .AsNoTracking()
            .Where(m => m.Id == occupationCodeId)
            .Select(m => (HazardClass?)m.HazardClass)
            .FirstOrDefaultAsync(cancellationToken);

        // The consistency check is skipped when the code is not found or the notice defines no hazard class.
        return hazardClass is null or HazardClass.Unspecified ? null : hazardClass;
    }
}
