using Ensa.Domain.Common;

namespace Ensa.Domain.Membership;

/// <summary>
/// A permission granted by default to a user type.
/// Legacy: <c>UserTypePermission_T</c>.
/// <para>
/// The legacy link went through <c>UserTypeCode</c> (string); it has been normalised to the
/// <see cref="UserTypeId"/> FK.
/// </para>
/// <para>
/// This is a host definition (a default that applies to every tenant) and does NOT implement
/// <see cref="IMultiTenant"/>. Tenant-level exceptions are granted or revoked through
/// <see cref="UserPermission"/>.
/// </para>
/// </summary>
public class UserTypePermission : AuditedEntity, IActivatable
{
    public int UserTypeId { get; set; }

    public int PermissionId { get; set; }

    /// <summary>(Legacy: Aktif — an inactive row means "permission revoked"; the row is not deleted.)</summary>
    public bool IsActive { get; set; } = true;
}
