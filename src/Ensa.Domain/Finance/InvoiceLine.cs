using Ensa.Domain.Common;

namespace Ensa.Domain.Finance;

/// <summary>
/// An invoice line. The VAT breakdown and totals of an <see cref="Invoice"/> are computed from
/// these lines.
/// <para>Legacy equivalent: <c>InvoiceLines_T</c>.</para>
/// </summary>
public class InvoiceLine : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The invoice this line belongs to. FK — no navigation property.</summary>
    public int InvoiceId { get; set; }

    /// <summary>The service item the line is based on, if any. (Legacy: <c>HizmetKalemi</c>) FK — no navigation property.</summary>
    public int? ServiceItemId { get; set; }

    public string LineDescription { get; set; } = string.Empty;

    /// <summary>Miktar. (Legacy: <c>int</c> → <c>decimal</c>; kesirli miktarlara izin verir.)</summary>
    public decimal Count { get; set; }

    public string Unit { get; set; } = string.Empty;

    /// <summary>Unit price, excluding VAT. (Legacy: <c>Tutar</c> <c>double</c> → <c>decimal</c>)</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Line total excluding VAT (<c>Count * UnitPrice</c>). (Legacy: <c>double</c> → <c>decimal</c>)</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>VAT rate, as a percentage. (Legacy: <c>Kdv</c>)</summary>
    public int VatRate { get; set; }

    /// <summary>VAT amount for the line. (Legacy: <c>double</c> → <c>decimal</c>)</summary>
    public decimal VatAmount { get; set; }

    /// <summary>Line total including VAT. (Legacy: <c>double</c> → <c>decimal</c>)</summary>
    public decimal GrossWithVatAmount { get; set; }

    /// <summary>Per-line company reference, carried over as nullable from legacy; it rarely differs from the header. FK — no navigation property.</summary>
    public int? CompanyId { get; set; }

    /// <summary>Line order on the invoice. Not present in legacy; added to give listings a stable order.</summary>
    public int OrderNo { get; set; }
}
