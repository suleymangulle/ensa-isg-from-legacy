using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Membership.Dtos.Navigations;

/// <summary>
/// The permission catalogue rendered as a hierarchy over <c>Permission.ParentPermissionId</c>.
/// <para>
/// A recursive shape cannot live on a plain DTO (no class-typed properties), so the tree is
/// exposed through <see cref="NavigationDto"/> derivatives, exactly like the menu tree.
/// </para>
/// </summary>
public class PermissionTreeDto : NavigationDto
{
    /// <summary>Root permissions (<c>ParentPermissionId == null</c>), ordered by sort order.</summary>
    public List<PermissionTreeNodeDto> Roots { get; set; } = [];

    /// <summary>Total number of nodes in the tree, roots and descendants together.</summary>
    public int TotalCount { get; set; }
}

/// <summary>A single node of the permission tree together with its children.</summary>
public class PermissionTreeNodeDto : NavigationDto
{
    public int Id { get; set; }

    public int? ParentPermissionId { get; set; }

    public PermissionType PermissionType { get; set; }

    /// <summary>Protected target name; matches an <c>EnsaPermissions</c> constant.</summary>
    public string PermissionTarget { get; set; } = string.Empty;

    public string PermissionName { get; set; } = string.Empty;

    public string? PermissionDescription { get; set; }

    public PermissionRestrictionMode PermissionRestrictionMode { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Child permissions, ordered. Empty for a leaf node.</summary>
    public List<PermissionTreeNodeDto> Children { get; set; } = [];

    /// <summary>Convenience flag for the client so it does not have to inspect the list.</summary>
    public bool HasChildren => Children.Count > 0;
}

/// <summary>
/// The permission picture of one user: what is in effect right now, and which explicit
/// overrides produced it.
/// </summary>
public class UserPermissionsDto : NavigationDto
{
    public int UserId { get; set; }

    /// <summary>
    /// Effective permissions as computed by <c>IPermissionManager</c>. This is the authoritative
    /// answer — the two override lists below only explain how it came about.
    /// </summary>
    public List<PermissionDto> EffectivePermissions { get; set; } = [];

    /// <summary>Permission ids explicitly granted to this user.</summary>
    public List<int> GrantedPermissionIds { get; set; } = [];

    /// <summary>Permission ids explicitly denied to this user; a denial beats every grant.</summary>
    public List<int> DeniedPermissionIds { get; set; } = [];

    /// <summary>
    /// <c>true</c> when the user is a system administrator, in which case the effective set is
    /// the whole catalogue and the override lists are irrelevant.
    /// </summary>
    public bool SystemAdministrator { get; set; }
}
/// <summary>
/// The permission defaults of one staff type.
/// <para>
/// This is the role-scoped half of authorization: a physician or a safety specialist gets these
/// simply by being that type, and a per-user override is only needed for an exception. Setting
/// them once here is what stops an administrator granting 171 permissions to every new user by
/// hand.
/// </para>
/// </summary>
public class UserTypePermissionsDto : NavigationDto
{
    public int UserTypeId { get; set; }

    public string UserTypeName { get; set; } = string.Empty;

    /// <summary>Permissions every user of this type receives.</summary>
    public List<PermissionDto> Permissions { get; set; } = [];

    /// <summary>Their ids, for a screen that only needs to tick boxes.</summary>
    public List<int> PermissionIds { get; set; } = [];
}
