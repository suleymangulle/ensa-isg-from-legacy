using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Finance;

/// <summary>
/// A sales or purchase invoice: the header of an invoice the organization (tenant) issued to — or
/// received from — a customer workplace. Its lines live in <see cref="InvoiceLine"/>.
/// <para>Legacy equivalent: <c>Faturalar_T</c>.</para>
/// <para>
/// NORMALIZATION: the fixed <c>Vat18</c>/<c>Vat8</c> rate columns were REMOVED from the header —
/// the VAT breakdown is now computed from the <c>VatRate</c>/<c>VatAmount</c> fields on the
/// <see cref="InvoiceLine"/> rows (see <c>IInvoiceManager</c>). An invoice is therefore no longer
/// limited to two fixed VAT rates: every line can carry its own (1%, 8%, 10%, 18%, 20% and so on).
/// </para>
/// <para>
/// The legacy <c>InvoiceAyi</c>/<c>InvoiceYear</c> (string) columns were REMOVED; both are derived
/// from <see cref="InvoiceDate"/> rather than stored.
/// </para>
/// </summary>
public class Invoice : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>Invoice number. Unique within a tenant and year (see <c>IInvoiceManager</c>).</summary>
    public string InvoiceNo { get; set; } = string.Empty;

    /// <summary>The customer workplace the invoice was issued to. FK — no navigation property.</summary>
    public int CompanyId { get; set; }

    public DateTime InvoiceDate { get; set; }

    /// <summary>Invoice direction. (Legacy: <c>Turu</c> string "Satış"/"Alış")</summary>
    public InvoiceType InvoiceType { get; set; }

    /// <summary>The module the invoice originated in. (Legacy: <c>Modul</c> string)</summary>
    public SourceModule SourceModule { get; set; } = SourceModule.Unspecified;

    /// <summary>The office or branch that issued the invoice. (Legacy: <c>Sube_ID</c>) FK — no navigation property.</summary>
    public int? OfficeId { get; set; }

    /// <summary>The account (customer) title printed on the invoice — free text, independent of <see cref="CompanyId"/>.</summary>
    public string AccountCurrentName { get; set; } = string.Empty;

    public string? InvoiceDescription { get; set; }

    /// <summary>The grand total spelled out in words, produced by <c>IInvoiceManager.AmountToWords</c>.</summary>
    public string? InWords { get; set; }

    /// <summary>Total excluding VAT, computed from the lines. (Legacy: <c>double</c> → <c>decimal</c>)</summary>
    public decimal Total { get; set; }

    /// <summary>Total VAT amount — the sum of every line's VAT breakdown.</summary>
    public decimal VatTotal { get; set; }

    /// <summary>KDV dahil genel toplam. (Legacy: <c>double</c> → <c>decimal</c>)</summary>
    public decimal GeneralTotal { get; set; }
}
