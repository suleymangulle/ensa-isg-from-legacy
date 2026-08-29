using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;

namespace Ensa.Application.Contracts.Companies.Dtos;

/// <summary>Workplace department list row.</summary>
public class WorkplaceDepartmentListDto : EntityDto
{
    public int CompanyId { get; set; }

    /// <summary>Requires a join; filled in by the repository/projection, not by AutoMapper.</summary>
    public string? CompanyName { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>False for system-generated default departments, which cannot be removed.</summary>
    public bool IsDeletable { get; set; }
}

/// <summary>Workplace department detail view.</summary>
public class WorkplaceDepartmentDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int CompanyId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public bool IsDeletable { get; set; }
}

/// <summary>Workplace department creation input.</summary>
public class CreateWorkplaceDepartmentDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A workplace must be selected.")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "The department name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string DepartmentName { get; set; } = string.Empty;
}

/// <summary>Workplace department update input.</summary>
public class UpdateWorkplaceDepartmentDto : CreateWorkplaceDepartmentDto;

/// <summary>Workplace department list filter.</summary>
public class GetWorkplaceDepartmentListInput : PagedAndSortedFilterDto
{
    /// <summary>Free-text search runs over the department name.</summary>
    public int? CompanyId { get; set; }

    public bool? IsDeletable { get; set; }
}
