using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Membership;

/// <summary>
/// Restricts a permission to specific user types.
/// Legacy: <c>YetkiKisit_T</c> (its tenant column was <c>OrganizationId</c>).
/// <para>
/// What a row means depends on the owning <see cref="Permission.PermissionRestrictionMode"/>
/// (legacy <c>YetkilendirmeController.PermissionRestrictionControl</c>):
/// <list type="bullet">
///   <item><see cref="PermissionRestrictionMode.OnlySelected"/> → the permission MAY be granted
///         to the user types listed here and to no others (allow list).</item>
///   <item><see cref="PermissionRestrictionMode.SelectedExcept"/> → the permission may NOT be
///         granted to the user types listed here (deny list).</item>
///   <item><see cref="PermissionRestrictionMode.Everyone"/> → rows in this table are ignored.</item>
/// </list>
/// </para>
/// </summary>
public class PermissionRestriction : AuditedTenantEntity
{
    public int PermissionId { get; set; }

    /// <summary>FK of the user type on the restriction list. (Legacy: KullaniciTypeId)</summary>
    public int UserTypeId { get; set; }
}
