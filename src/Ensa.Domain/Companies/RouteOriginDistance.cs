using Ensa.Domain.Common;

namespace Ensa.Domain.Companies;

/// <summary>
/// Road distance between a <see cref="RouteOrigin"/> and a company (a cached record).
/// It is computed by the mapping service and used when planning visit routes.
/// <para>Legacy equivalent: <c>OrginCompanyDistance_T</c>.</para>
/// </summary>
public class RouteOriginDistance : CreationAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The origin. May be <c>null</c> for coarse, province-level distances.</summary>
    public int? OriginId { get; set; }

    /// <summary>Province name, when the distance was computed from a province rather than an origin record.</summary>
    public string? CityName { get; set; }

    public int CompanyId { get; set; }

    /// <summary>Distance in kilometres. (Legacy: <c>double</c>)</summary>
    public decimal DistanceKm { get; set; }
}
