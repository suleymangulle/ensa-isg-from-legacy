using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Tenancy.Dtos.Navigations;

/// <summary>
/// Everything the office detail screen needs in a single call — the office, its
/// organization, its location and the attached counters.
/// <para>
/// Mirrors <c>Ensa.Domain.Tenancy.Navigations.OfficeNavigation</c>, plus the cash
/// register counter the finance screens need.
/// </para>
/// </summary>
public class OfficeNavigationDto : NavigationDto
{
    public OfficeDto Office { get; set; } = null!;

    public LookupDto? Organization { get; set; }

    public LookupDto? City { get; set; }

    public LookupDto? District { get; set; }

    /// <summary>Active users assigned to the office.</summary>
    public int UserCount { get; set; }

    /// <summary>Active cash registers attached to the office.</summary>
    public int CashRegisterCount { get; set; }
}
