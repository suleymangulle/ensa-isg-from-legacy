using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Membership.Dtos;

/// <summary>
/// A single permission definition from the host permission catalogue.
/// <para>
/// The catalogue itself is seeded from <c>EnsaPermissions</c> and is not editable through
/// the API, so there is no create/update input DTO for it.
/// </para>
/// </summary>
public class PermissionDto : EntityDto
{
    /// <summary>Parent permission for grouping. <c>null</c> marks a root permission.</summary>
    public int? ParentPermissionId { get; set; }

    public PermissionType PermissionType { get; set; }

    /// <summary>Protected target name; matches an <c>EnsaPermissions</c> constant.</summary>
    public string PermissionTarget { get; set; } = string.Empty;

    /// <summary>Display name shown on the permission screen.</summary>
    public string PermissionName { get; set; } = string.Empty;

    public string? PermissionDescription { get; set; }

    /// <summary>Custom message shown to the user when the permission is denied.</summary>
    public string? RedMessage { get; set; }

    public PermissionRestrictionMode PermissionRestrictionMode { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>Permission list filter.</summary>
public class GetPermissionListInput : PagedAndSortedFilterDto
{
    public PermissionType? PermissionType { get; set; }
    public int? ParentPermissionId { get; set; }
}

/// <summary>
/// Replaces the explicit permission overrides of one user.
/// <para>
/// Both collections are absolute — whatever is not listed is removed. A permission id may
/// not appear in both lists; the service rejects such a payload.
/// </para>
/// </summary>
public class UpdateUserPermissionsDto
{
    /// <summary>Permissions explicitly granted to the user on top of the staff-role defaults.</summary>
    public int[] GrantedPermissionIds { get; set; } = [];

    /// <summary>
    /// Permissions explicitly denied. A denial always wins over any grant, including
    /// grants inherited from the staff role.
    /// </summary>
    public int[] DeniedPermissionIds { get; set; } = [];
}
/// <summary>
/// Replaces the permission defaults of a staff type.
/// <para>
/// The list is absolute, not a delta: what is sent becomes the whole set. There is no deny list
/// here — a default is either given to the type or not, and an exception for one person belongs
/// on that person (<see cref="UpdateUserPermissionsDto"/>).
/// </para>
/// </summary>
public class UpdateUserTypePermissionsDto
{
    public int[] PermissionIds { get; set; } = [];
}
