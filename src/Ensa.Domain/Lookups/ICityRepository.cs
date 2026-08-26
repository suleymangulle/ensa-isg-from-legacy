using Ensa.Domain.Repositories;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Module-specific repository contract for <see cref="City"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface ICityRepository : IRepository<City>
{
    /// <summary>Districts belonging to a province.</summary>
    Task<List<District>> GetDistrictsAsync(int cityId, CancellationToken cancellationToken = default);

    /// <summary>Neighbourhoods belonging to a district.</summary>
    Task<List<Neighborhood>> GetNeighborhoodsAsync(int districtId, CancellationToken cancellationToken = default);
}
