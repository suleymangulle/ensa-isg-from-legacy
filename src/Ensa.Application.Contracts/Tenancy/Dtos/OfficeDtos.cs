using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;

namespace Ensa.Application.Contracts.Tenancy.Dtos;

/// <summary>Office list row.</summary>
public class OfficeListDto : EntityDto
{
    /// <summary>Resolved by the application service with one batched query per page.</summary>
    public string? CompanyName { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public int? CityId { get; set; }

    /// <summary>Requires a join; filled in by the repository/projection, not by AutoMapper.</summary>
    public string? CityName { get; set; }

    /// <summary>Requires a join; filled in by the repository/projection, not by AutoMapper.</summary>
    public string? DistrictName { get; set; }

    public string? AuthorizedPerson { get; set; }

    public int? CompanyId { get; set; }

    public bool HeadquarterOffice { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>Office detail view.</summary>
public class OfficeDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? Fax { get; set; }

    public string? Address { get; set; }
    public int? CityId { get; set; }
    public int? DistrictId { get; set; }

    public string? AuthorizedPerson { get; set; }
    public string? AuthorizedEmail { get; set; }

    /// <summary><c>null</c> means the office is attached directly to the organization.</summary>
    public int? CompanyId { get; set; }

    /// <summary>Only one office per organization may carry this flag.</summary>
    public bool HeadquarterOffice { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>Office creation input.</summary>
public class CreateOfficeDto
{
    [Required(ErrorMessage = "The office name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? Phone { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? Fax { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Address)]
    public string? Address { get; set; }

    public int? CityId { get; set; }

    public int? DistrictId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string? AuthorizedPerson { get; set; }

    [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Email)]
    public string? AuthorizedEmail { get; set; }

    /// <summary>Leave empty for an office attached directly to the organization.</summary>
    public int? CompanyId { get; set; }

    /// <summary>
    /// Marks the organization's headquarters office. The service refuses the request when
    /// another office already holds the flag.
    /// </summary>
    public bool HeadquarterOffice { get; set; }
}

/// <summary>Office update input.</summary>
public class UpdateOfficeDto : CreateOfficeDto
{
    public bool IsActive { get; set; } = true;
}

/// <summary>Office list filter.</summary>
public class GetOfficeListInput : PagedAndSortedFilterDto
{
    /// <summary>Free-text search runs over the office name and the authorized person.</summary>
    public int? CityId { get; set; }

    public int? CompanyId { get; set; }

    public bool? HeadquarterOffice { get; set; }

    public bool? IsActive { get; set; }
}
