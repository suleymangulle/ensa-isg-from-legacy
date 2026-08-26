using Ensa.Domain.Common;

namespace Ensa.Domain.Membership;

/// <summary>
/// A permission included in a subscription plan. Legacy: <c>PaketTuruYetki_T</c>.
/// <para>
/// This is a MANDATORY GATE in the permission calculation: if the permission is not part of the
/// purchased plan, it does not take effect even when it has been granted to the user
/// individually (legacy: "Bu eylem satın alınan paket dışı kalmaktadır...").
/// </para>
/// <para>This is a host definition and does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class SubscriptionPlanPermission : AuditedEntity
{
    public int SubscriptionPlanId { get; set; }

    public int PermissionId { get; set; }
}
