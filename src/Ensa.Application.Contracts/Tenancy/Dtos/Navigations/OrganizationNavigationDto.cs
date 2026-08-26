using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Tenancy.Dtos.Navigations;

/// <summary>
/// Everything the organization detail screen needs in a single call — the organization,
/// its type and plan, its location, its offices and the quota counters.
/// <para>
/// Mirrors <c>Ensa.Domain.Tenancy.Navigations.OrganizationNavigation</c>.
/// </para>
/// </summary>
public class OrganizationNavigationDto : NavigationDto
{
    public OrganizationDto Organization { get; set; } = null!;

    public LookupDto? OrganizationType { get; set; }

    public LookupDto? SubscriptionPlan { get; set; }

    public LookupDto? City { get; set; }

    public LookupDto? District { get; set; }

    /// <summary>Offices defined for the organization.</summary>
    public List<LookupDto> Offices { get; set; } = [];

    /// <summary>The organization's headquarters office, when one is flagged.</summary>
    public LookupDto? HeadquarterOffice { get; set; }

    /// <summary>The subscription contract currently in force.</summary>
    public OrganizationContractSummaryDto? CurrentContract { get; set; }

    public int OfficeCount { get; set; }

    /// <summary>Active users in the organization — used for the plan quota check.</summary>
    public int ActiveUserCount { get; set; }

    /// <summary>Active workplaces in the organization — used for the plan quota check.</summary>
    public int ActiveCompanyCount { get; set; }
}

/// <summary>Condensed view of the subscription contract in force.</summary>
public class OrganizationContractSummaryDto : EntityDto
{
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;

    public DateTime ContractDate { get; set; }

    public decimal UnitPrice { get; set; }
    public int UserCount { get; set; }
    public decimal TotalPrice { get; set; }

    public int? SubscriptionPlanId { get; set; }
    public int? OrganizationTypeId { get; set; }

    public bool IsApproved { get; set; }
    public bool Paid { get; set; }
    public bool IsActive { get; set; }

    public ContractStatus ContractStatus { get; set; }
    public DateTime? ContractStatusDate { get; set; }

    /// <summary>Populated once the account has been closed; the subscription has ended.</summary>
    public DateTime? AccountClosingDate { get; set; }
}
