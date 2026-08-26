using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups.Navigations;

/// <summary>
/// Combined read model for a district together with its province and neighbourhoods.
/// <para>
/// <c>[NotMapped]</c> — never exposed as a <c>DbSet</c> and never registered with
/// <c>ModelBuilder</c>; it is populated in the repository layer through an
/// <c>IQueryable</c> join plus projection.
/// </para>
/// </summary>
[NotMapped]
public class DistrictNavigation : NavigationEntity<District>
{
    /// <summary>Shortcut to the root record (the same instance as <see cref="NavigationEntity{TEntity}.Entity"/>).</summary>
    public District District
    {
        get => Entity;
        set => Entity = value;
    }

    /// <summary>The province this district belongs to.</summary>
    public City? City { get; set; }

    /// <summary>Neighbourhoods belonging to this district.</summary>
    public List<Neighborhood> Neighborhoods { get; set; } = [];
}
