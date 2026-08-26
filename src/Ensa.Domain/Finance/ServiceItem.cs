using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Finance;

/// <summary>
/// A billable service item — a price-list entry. Invoice lines
/// (<see cref="InvoiceLine.ServiceItemId"/>) are chosen from here.
/// <para>Legacy equivalent: <c>ServiceKartlari_T</c>.</para>
/// </summary>
public class ServiceItem : FullAuditedTenantEntity, IActivatable
{
    public string Code { get; set; } = string.Empty;

    /// <summary>Service name. (Legacy: <c>HizmetKarti</c>)</summary>
    public string Name { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    /// <summary>Default quantity suggested when the item is added to an invoice line. (Legacy: <c>DefaultDeger</c>)</summary>
    public int DefaultValue { get; set; }

    public int VatRate { get; set; }

    public ServiceItemType CardType { get; set; } = ServiceItemType.Unspecified;

    /// <summary>(Not present in legacy; added so that an item can be deactivated.)</summary>
    public bool IsActive { get; set; } = true;
}
