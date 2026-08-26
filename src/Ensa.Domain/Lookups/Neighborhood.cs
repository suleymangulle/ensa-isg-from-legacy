using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Neighbourhood reference record.
/// <para>Legacy equivalent: <c>Neighborhood_T</c>.</para>
/// <para>
/// Host-level (tenant-less) read-only reference table; the legacy table carried no audit
/// columns, so plain <see cref="Entity"/> is used as the base class.
/// </para>
/// </summary>
public class Neighborhood : Entity
{
    /// <summary>Neighbourhood name.</summary>
    public string NeighborhoodName { get; set; } = string.Empty;

    /// <summary>Owning district. FK — no navigation property.</summary>
    public int DistrictId { get; set; }
}
