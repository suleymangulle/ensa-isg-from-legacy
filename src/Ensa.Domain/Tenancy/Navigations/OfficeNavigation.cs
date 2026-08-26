using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;

namespace Ensa.Domain.Tenancy.Navigations;

/// <summary>
/// Combined view of an office with its organization, location and user count.
/// <para><c>[NotMapped]</c> — it is not a <c>DbSet</c>; it is filled by projection.</para>
/// </summary>
[NotMapped]
public class OfficeNavigation : NavigationEntity<Office>
{
    /// <summary>Shortcut to the root record.</summary>
    public Office Office
    {
        get => Entity;
        set => Entity = value;
    }

    public Organization? Organization { get; set; }

    public string? CityName { get; set; }

    public string? DistrictName { get; set; }

    /// <summary>The number of active users assigned to the office.</summary>
    public int UserCount { get; set; }
}
