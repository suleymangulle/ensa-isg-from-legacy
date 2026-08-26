using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups.Navigations;

/// <summary>
/// Combined read model for a province and its districts.
/// <para>
/// <c>[NotMapped]</c> — never exposed as a <c>DbSet</c> and never registered with
/// <c>ModelBuilder</c>; it is populated in the repository layer through an
/// <c>IQueryable</c> join plus projection.
/// </para>
/// </summary>
[NotMapped]
public class CityNavigation : NavigationEntity<City>
{
    /// <summary>Shortcut to the root record (the same instance as <see cref="NavigationEntity{TEntity}.Entity"/>).</summary>
    public City City
    {
        get => Entity;
        set => Entity = value;
    }

    /// <summary>Districts belonging to this province.</summary>
    public List<District> Districts { get; set; } = [];
}
