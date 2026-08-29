using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;

namespace Ensa.Application.Contracts.Tenancy.Dtos;

/// <summary>Organization (tenant) list row.</summary>
public class OrganizationListDto : EntityDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public int OrganizationTypeId { get; set; }
    public int SubscriptionPlanId { get; set; }

    /// <summary>Requires a join; filled in by the repository/projection, not by AutoMapper.</summary>
    public string? OrganizationTypeName { get; set; }

    /// <summary>Requires a join; filled in by the repository/projection, not by AutoMapper.</summary>
    public string? SubscriptionPlanName { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }

    public DateTime SubscriptionStart { get; set; }
    public DateTime? SubscriptionEnd { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>
/// Organization (tenant) detail view.
/// <para>
/// <c>Organization</c> is a host entity — it does not implement <c>IMultiTenant</c> — so this
/// DTO carries no <c>TenantId</c>. Its <c>Id</c> is what every other entity's
/// <c>TenantId</c> points at.
/// </para>
/// </summary>
public class OrganizationDto : FullAuditedEntityDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public int OrganizationTypeId { get; set; }
    public int SubscriptionPlanId { get; set; }

    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }

    public string? Address { get; set; }
    public int? CityId { get; set; }
    public int? DistrictId { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WebUrl { get; set; }

    public string? AuthorizedFullName { get; set; }
    public string? AuthorizedPhone { get; set; }
    public string? AuthorizedEmail { get; set; }

    public int? LogoDocumentId { get; set; }

    public bool IsActive { get; set; }

    public DateTime SubscriptionStart { get; set; }

    /// <summary><c>null</c> means the subscription never expires.</summary>
    public DateTime? SubscriptionEnd { get; set; }

    /// <summary>Plan quota — <c>null</c> means unlimited.</summary>
    public int? MaximumUserCount { get; set; }

    /// <summary>Plan quota — <c>null</c> means unlimited.</summary>
    public int? MaximumCompanyCount { get; set; }
}

/// <summary>Organization creation input.</summary>
public class CreateOrganizationDto
{
    [Required(ErrorMessage = "The organization name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique organization code; used for tenant resolution and sub-domains.</summary>
    [Required(ErrorMessage = "The organization code is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string Code { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "An organization type must be selected.")]
    public int OrganizationTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A subscription plan must be selected.")]
    public int SubscriptionPlanId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? TaxOffice { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.TaxNo)]
    public string? TaxNumber { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Address)]
    public string? Address { get; set; }

    public int? CityId { get; set; }

    public int? DistrictId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Email)]
    public string? Email { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Url)]
    public string? WebUrl { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? AuthorizedFullName { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? AuthorizedPhone { get; set; }

    [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Email)]
    public string? AuthorizedEmail { get; set; }

    public int? LogoDocumentId { get; set; }

    public DateTime SubscriptionStart { get; set; }

    /// <summary>Leave empty for an open-ended subscription.</summary>
    public DateTime? SubscriptionEnd { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The user quota must be a positive number.")]
    public int? MaximumUserCount { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The workplace quota must be a positive number.")]
    public int? MaximumCompanyCount { get; set; }
}

/// <summary>Organization update input.</summary>
public class UpdateOrganizationDto : CreateOrganizationDto
{
    public bool IsActive { get; set; } = true;
}

/// <summary>Organization list filter.</summary>
public class GetOrganizationListInput : PagedAndSortedFilterDto
{
    /// <summary>Free-text search runs over the name, the code and the tax number.</summary>
    public int? OrganizationTypeId { get; set; }

    public int? SubscriptionPlanId { get; set; }

    public int? CityId { get; set; }

    public bool? IsActive { get; set; }
}
