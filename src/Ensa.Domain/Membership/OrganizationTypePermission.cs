using Ensa.Domain.Common;

namespace Ensa.Domain.Membership;

/// <summary>
/// A permission opened up to an organization type. Legacy: <c>KurumTuruYetki_T</c>.
/// <para>
/// This is a MANDATORY GATE in the permission calculation: if the permission is not opened for
/// the organization's type, it does not take effect even when it has been granted to the user
/// individually (legacy <c>PermissionCheck.Authorize</c> → <c>!OrganizationTypePermission</c> →
/// access denied).
/// </para>
/// <para>This is a host definition and does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class OrganizationTypePermission : AuditedEntity
{
    public int OrganizationTypeId { get; set; }

    public int PermissionId { get; set; }
}
