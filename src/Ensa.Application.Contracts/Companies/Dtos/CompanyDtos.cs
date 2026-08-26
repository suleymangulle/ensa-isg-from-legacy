using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Companies.Dtos;

/// <summary>One row of the company list — the fields shown in the table.</summary>
public class CompanyListDto : EntityDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string? SsiNumber { get; set; }
    public HazardClass HazardClass { get; set; }
    public WorkplaceType WorkplaceType { get; set; }
    public string? CityName { get; set; }
    public string? DistrictName { get; set; }
    public string? Phone { get; set; }
    public string? AuthorizedPerson { get; set; }
    public int? WorkerCount { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Company detail view.</summary>
public class CompanyDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string? SsiNumber { get; set; }
    public HazardClass HazardClass { get; set; }
    public WorkplaceType WorkplaceType { get; set; }
    public int? HeadquarterCompanyId { get; set; }

    public string? TaxTaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string? BusinessActivity { get; set; }
    public int? OccupationCodeId { get; set; }

    public string? Address { get; set; }
    public int CityId { get; set; }
    public int? DistrictId { get; set; }
    public int? NeighborhoodId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Gsm { get; set; }
    public string? Email { get; set; }
    public string? WebUrl { get; set; }

    public string? AuthorizedPerson { get; set; }
    public string? AuthorizedPersonPhone { get; set; }
    public string? AuthorizedPersonEmail { get; set; }
    public string? EmployerName { get; set; }

    public int? OfficeId { get; set; }
    public int? OrganizationTypeId { get; set; }
    public int? SubscriptionPlanId { get; set; }
    public int? LogoDocumentId { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Input for creating a company.</summary>
public class CreateCompanyDto
{
    [Required(ErrorMessage = "Company name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? SsiNumber { get; set; }

    public HazardClass HazardClass { get; set; } = HazardClass.Unspecified;

    public WorkplaceType WorkplaceType { get; set; } = WorkplaceType.Headquarter;

    /// <summary>Required for a branch; validated by <c>CompanyManager</c>.</summary>
    public int? HeadquarterCompanyId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A city must be selected.")]
    public int CityId { get; set; }

    public int? DistrictId { get; set; }
    public int? NeighborhoodId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Address)]
    public string? Address { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Email)]
    public string? Email { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? AuthorizedPerson { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.TaxNo)]
    public string? TaxNumber { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? TaxTaxOffice { get; set; }

    public int? OfficeId { get; set; }
    public int? OccupationCodeId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? Notes { get; set; }
}

/// <summary>Input for updating a company.</summary>
public class UpdateCompanyDto : CreateCompanyDto
{
    public bool IsActive { get; set; } = true;
}

/// <summary>Filter for the company list.</summary>
public class GetCompanyListInput : PagedAndSortedFilterDto
{
    public HazardClass? HazardClass { get; set; }
    public int? CityId { get; set; }
    public int? OfficeId { get; set; }
    public int? HeadquarterCompanyId { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>Only the companies this user is an assigned specialist for.</summary>
    public int? AssignedSpecialistUserId { get; set; }
}
