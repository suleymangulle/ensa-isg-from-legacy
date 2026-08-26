using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Membership;

/// <summary>
/// Permission definition — an access right at page or method level.
/// Legacy: <c>Yetki_T</c>.
/// <para>
/// This is a host reference table (it appears in the tenant-less table list of
/// ARCHITECTURE §5) and does NOT implement <see cref="IMultiTenant"/>. Granting a permission
/// to a user or an organization happens through the <see cref="UserPermission"/>,
/// <see cref="UserTypePermission"/>, <c>OrganizationTypePermission</c> and
/// <c>SubscriptionPlanPermission</c> tables.
/// </para>
/// </summary>
public class Permission : AuditedEntity, IHasSortOrder
{
    /// <summary>Parent permission FK used for hierarchical grouping. <c>null</c> = root permission.</summary>
    public int? ParentPermissionId { get; set; }

    /// <summary>(Legacy: YetkiTuru string — "sayfa-yetkisi"/"method-yetkisi")</summary>
    public PermissionType PermissionType { get; set; } = PermissionType.PagePermission;

    /// <summary>
    /// Full name of the protected target. In the legacy system this was a type/method name
    /// produced by reflection (<c>sf.GetMethod().ToString()</c> / <c>DeclaringType.FullName</c>).
    /// In the new architecture it matches an <c>EnsaPermissions</c> constant
    /// (e.g. "Ensa.Company.Create").
    /// (Legacy: YetkiHedefi)
    /// </summary>
    public string PermissionTarget { get; set; } = string.Empty;

    /// <summary>Permission name shown on screen. (Legacy: YetkiAdi)</summary>
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>(Legacy: YetkiAciklamasi)</summary>
    public string? PermissionDescription { get; set; }

    /// <summary>Custom message shown to the user when the permission is denied. (Legacy: Message)</summary>
    public string? RedMessage { get; set; }

    /// <summary>
    /// Restriction mode that determines which user types this permission may be granted to.
    /// The selected types are listed in the <see cref="PermissionRestriction"/> table.
    /// (Legacy: YetkiKisitHedef string)
    /// </summary>
    public PermissionRestrictionMode PermissionRestrictionMode { get; set; } = PermissionRestrictionMode.Everyone;

    public int SortOrder { get; set; }
}
