using Ensa.Domain.Common;

namespace Ensa.Domain.Membership;

/// <summary>
/// An explicit GRANT or DENY of a permission for a single user.
/// Legacy: <c>KullaniciYetki_T</c> (its tenant column was <c>OrganizationId</c>).
/// <para>
/// The <see cref="Authorized"/> flag works in both directions and is the core of the legacy
/// permission algorithm:
/// <list type="bullet">
///   <item><c>true</c> → the permission is explicitly GRANTED even if the user's type does not
///         carry it.</item>
///   <item><c>false</c> → the permission is explicitly DENIED even if the user's type carries it
///         (deny wins).</item>
/// </list>
/// Only rows with <c>IsActive == true</c> take part in the calculation.
/// </para>
/// </summary>
public class UserPermission : AuditedTenantEntity, IActivatable
{
    public int UserId { get; set; }

    public int PermissionId { get; set; }

    /// <summary><c>true</c> = explicit grant, <c>false</c> = explicit deny. (Legacy: Yetkili)</summary>
    public bool Authorized { get; set; }

    /// <summary>(Legacy: Aktif — inactive rows are excluded from the permission calculation.)</summary>
    public bool IsActive { get; set; } = true;
}
