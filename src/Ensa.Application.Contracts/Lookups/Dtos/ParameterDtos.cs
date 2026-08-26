using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;

namespace Ensa.Application.Contracts.Lookups.Dtos;

/// <summary>Parameter list row.</summary>
public class ParameterListDto : EntityDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>Parameter detail view - a per-organization key/value system setting.</summary>
public class ParameterDto : AuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>Fields shared by parameter create and update.</summary>
public abstract class ParameterInputDto
{
    [Required]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string Value { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

/// <summary>Parameter create input.</summary>
public class CreateParameterDto : ParameterInputDto
{
    /// <summary>
    /// Unique code within the organization. Immutable once created - application code reads
    /// parameters by this code, so renaming one would silently change behaviour elsewhere.
    /// </summary>
    [Required]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string Code { get; set; } = string.Empty;
}

/// <summary>Parameter update input. The code cannot be changed.</summary>
public class UpdateParameterDto : ParameterInputDto;

/// <summary>Parameter list filter.</summary>
public class GetParameterListInput : PagedAndSortedFilterDto
{
    public bool? IsActive { get; set; }
}

/// <summary>Result of a single parameter value read.</summary>
public class ParameterValueDto
{
    public string Code { get; set; } = string.Empty;

    /// <summary><c>null</c> when no parameter is defined for the code.</summary>
    public string? Value { get; set; }

    /// <summary><c>false</c> when the code is not defined for the current organization.</summary>
    public bool Exists { get; set; }
}
