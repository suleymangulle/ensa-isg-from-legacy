using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Province (city) reference record — Turkish administrative geography.
/// <para>Legacy equivalent: <c>City_T</c>.</para>
/// <para>
/// Shared (host) reference table for all tenants; it does NOT implement
/// <see cref="Common.IMultiTenant"/>.
/// </para>
/// </summary>
public class City : AuditedEntity
{
    /// <summary>Province name.</summary>
    public string CityName { get; set; } = string.Empty;

    /// <summary>Licence plate code (1-81).</summary>
    public int PlateCodeCode { get; set; }
}
