using Ensa.Domain.Common;

namespace Ensa.Domain.Companies;

/// <summary>
/// Starting point (origin) of a visit route. Distances to companies are measured from here.
/// <para>Legacy equivalent: <c>CompanyOrgin_T</c>.</para>
/// </summary>
public class RouteOrigin : CreationAuditedTenantEntity
{
    /// <summary>Name of the origin (e.g. "head office").</summary>
    public string? Tag { get; set; }

    public int CityId { get; set; }

    public int? DistrictId { get; set; }

    public string? Address { get; set; }
}
