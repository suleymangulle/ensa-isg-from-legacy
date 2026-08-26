using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;

namespace Ensa.Application.Contracts.Membership.Dtos;

/// <summary>Role list row.</summary>
public class RoleListDto : EntityDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>System-defined role: it can be neither renamed nor deleted.</summary>
    public bool IsStatic { get; set; }

    /// <summary>Automatically assigned to newly created users.</summary>
    public bool IsDefault { get; set; }

    /// <summary><c>null</c> means a host role shared by every organization.</summary>
    public int? TenantId { get; set; }
}

/// <summary>Role detail view.</summary>
public class RoleDto : EntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsStatic { get; set; }
    public bool IsDefault { get; set; }

    /// <summary>Number of users currently holding this role.</summary>
    public int UserCount { get; set; }
}

/// <summary>Fields shared by role create and update.</summary>
public abstract class RoleInputDto
{
    [Required]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.ShortName)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Description)]
    public string? Description { get; set; }

    public bool IsDefault { get; set; }
}

/// <summary>Role create input.</summary>
public class CreateRoleDto : RoleInputDto;

/// <summary>
/// Role update input. <see cref="RoleInputDto.Name"/> is ignored for static roles —
/// the service rejects the attempt with <c>Ensa:Role:SystemRoleImmutable</c>.
/// </summary>
public class UpdateRoleDto : RoleInputDto;

/// <summary>Role list filter.</summary>
public class GetRoleListInput : PagedAndSortedFilterDto
{
    public bool? IsStatic { get; set; }
    public bool? IsDefault { get; set; }
}
