using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Membership;

/// <summary>
/// Binds a permission to an object such as a module, user type, account, menu or menu item.
/// Legacy: <c>YetkiBaglanti_T</c> (plus its <c>LinkType</c> enum).
/// <para>
/// The target is identified either by id (<see cref="LinkTargetId"/>) or by code
/// (<see cref="LinkTargetCode"/>) — the legacy type had two constructor overloads for this.
/// </para>
/// <para>This is a host definition and does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class PermissionScope : AuditedEntity, IActivatable
{
    /// <summary>(Legacy: BaglantiType — the values match the legacy enum one to one.)</summary>
    public PermissionScopeType LinkType { get; set; }

    /// <summary>Id of the target object. (Legacy: BaglantiTypeId)</summary>
    public int? LinkTargetId { get; set; }

    /// <summary>Code of the target object, for targets bound by code instead of id. (Legacy: BaglantiTypeString)</summary>
    public string? LinkTargetCode { get; set; }

    public int? PermissionId { get; set; }

    /// <summary>(Legacy: Aktif)</summary>
    public bool IsActive { get; set; } = true;
}
