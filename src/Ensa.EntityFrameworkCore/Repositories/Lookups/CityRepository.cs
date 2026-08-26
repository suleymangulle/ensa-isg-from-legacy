using Ensa.Domain.Common;
using Ensa.Domain.Lookups;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Lookups;

/// <summary>
/// Queries specific to the <see cref="City"/> module.
/// <para>
/// <see cref="City"/>, <see cref="District"/> and <see cref="Neighborhood"/> are host reference tables
/// (they do not implement <c>IMultiTenant</c>), so no tenant predicate is written in these queries.
/// </para>
/// </summary>
public class CityRepository(EnsaDbContext context, IDataFilter dataFilter)
    : EfCoreRepository<City>(context, dataFilter), ICityRepository
{
    /// <inheritdoc />
    public Task<List<District>> GetDistrictsAsync(int cityId, CancellationToken cancellationToken = default)
        => Context.Set<District>()
                  .AsNoTracking()
                  .Where(i => i.CityId == cityId)
                  .OrderBy(i => i.DistrictName)
                  .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<List<Neighborhood>> GetNeighborhoodsAsync(int districtId, CancellationToken cancellationToken = default)
        => Context.Set<Neighborhood>()
                  .AsNoTracking()
                  .Where(m => m.DistrictId == districtId)
                  .OrderBy(m => m.NeighborhoodName)
                  .ToListAsync(cancellationToken);
}
