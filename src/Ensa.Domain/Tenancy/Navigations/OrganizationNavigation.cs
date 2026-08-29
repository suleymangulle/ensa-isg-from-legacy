using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;

namespace Ensa.Domain.Tenancy.Navigations;

/// <summary>
/// Combined view of an organization with its type, plan and offices.
/// <para>
/// <c>[NotMapped]</c> — NEVER a <c>DbSet</c>, never added to <c>ModelBuilder</c>;
/// populated in the repository layer through an <c>IQueryable</c> join and projection.
/// </para>
/// </summary>
[NotMapped]
public class OrganizationNavigation : NavigationEntity<Organization>
{
    /// <summary>Shortcut to the root record (the same object as <see cref="NavigationEntity{TEntity}.Entity"/>).</summary>
    public Organization Organization
    {
        get => Entity;
        set => Entity = value;
    }

    public OrganizationType? OrganizationType { get; set; }

    public SubscriptionPlan? SubscriptionPlan { get; set; }

    /// <summary>The offices defined for the organization.</summary>
    public List<Office> Offices { get; set; } = [];

    /// <summary>Kurumun merkez ofisi (<c>Office.IsHeadquarterOffice == true</c>).</summary>
    public Office? HeadquarterOffice { get; set; }

    /// <summary>The subscription contract currently in force.</summary>
    public OrganizationContract? CurrentContract { get; set; }

    /// <summary>City name; a lookup, since the <c>City</c> table is defined in another module.</summary>
    public string? CityName { get; set; }

    /// <summary>District name; a lookup.</summary>
    public string? DistrictName { get; set; }

    /// <summary>The number of active users in the organization, for quota checks.</summary>
    public int ActiveUserCount { get; set; }

    /// <summary>The number of active companies under the organization, for quota checks.</summary>
    public int ActiveCompanyCount { get; set; }
}
