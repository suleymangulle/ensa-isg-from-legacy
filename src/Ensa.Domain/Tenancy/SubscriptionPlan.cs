using Ensa.Domain.Common;

namespace Ensa.Domain.Tenancy;

/// <summary>
/// Reference table of subscription plans.
/// Legacy: <c>PaketTuru_T</c>.
/// <para>Host table — shared by every tenant; it does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class SubscriptionPlan : AuditedEntity, IActivatable, IHasSortOrder
{
    /// <summary>Unique code. (Legacy: PaketTuruKodu — Menu_T and Firma_T matched on it.)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name. (Legacy: PaketTuruAdi)</summary>
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
