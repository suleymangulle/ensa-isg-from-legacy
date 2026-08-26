using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Finance.Dtos;

/// <summary>Invoice list row — the columns shown in the grid.</summary>
public class InvoiceListDto : EntityDto
{
    public string InvoiceNo { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string AccountCurrentName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public InvoiceType InvoiceType { get; set; }
    public SourceModule SourceModule { get; set; }
    public int? OfficeId { get; set; }
    public decimal Total { get; set; }
    public decimal VatTotal { get; set; }
    public decimal GeneralTotal { get; set; }
}

/// <summary>Invoice header detail view.</summary>
public class InvoiceDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string InvoiceNo { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public InvoiceType InvoiceType { get; set; }
    public SourceModule SourceModule { get; set; }
    public int? OfficeId { get; set; }
    public string AccountCurrentName { get; set; } = string.Empty;
    public string? InvoiceDescription { get; set; }

    /// <summary>Grand total spelled out in words. Produced by <c>IInvoiceManager.AmountToWords</c>.</summary>
    public string? InWords { get; set; }

    /// <summary>Net total, VAT excluded. Always recomputed from the lines; never accepted from the client.</summary>
    public decimal Total { get; set; }

    /// <summary>Sum of the per-line VAT amounts.</summary>
    public decimal VatTotal { get; set; }

    /// <summary>Gross total, VAT included.</summary>
    public decimal GeneralTotal { get; set; }
}

/// <summary>A single invoice line.</summary>
public class InvoiceLineDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int InvoiceId { get; set; }
    public int? ServiceItemId { get; set; }
    public string LineDescription { get; set; } = string.Empty;
    public decimal Count { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public int VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal GrossWithVatAmount { get; set; }
    public int? CompanyId { get; set; }
    public int OrderNo { get; set; }
}

/// <summary>
/// Invoice header creation input.
/// <para>
/// Lines are NOT part of this payload: an invoice is created as an empty header and its lines
/// are then managed through the dedicated line endpoints. Every line change re-runs
/// <c>IInvoiceManager.CalculateInvoiceTotals</c>, so the header totals can never drift from the
/// lines and are never taken from the client.
/// </para>
/// </summary>
public class CreateInvoiceDto
{
    /// <summary>Leave empty to have the next number generated for the office and year.</summary>
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? InvoiceNo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A workplace must be selected.")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "The invoice date is required.")]
    public DateTime InvoiceDate { get; set; }

    public InvoiceType InvoiceType { get; set; } = InvoiceType.Sale;

    public SourceModule SourceModule { get; set; } = SourceModule.Manual;

    public int? OfficeId { get; set; }

    [Required(ErrorMessage = "The account title is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string AccountCurrentName { get; set; } = string.Empty;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? InvoiceDescription { get; set; }
}

/// <summary>Invoice header update input.</summary>
public class UpdateInvoiceDto : CreateInvoiceDto;

/// <summary>Invoice line creation input. Line and header totals are computed server-side.</summary>
public class CreateInvoiceLineDto
{
    public int? ServiceItemId { get; set; }

    [Required(ErrorMessage = "The line description is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string LineDescription { get; set; } = string.Empty;

    [Range(0.0001, 9999999999.9999, ErrorMessage = "The quantity must be greater than zero.")]
    public decimal Count { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string Unit { get; set; } = string.Empty;

    [Range(0, 9999999999.99, ErrorMessage = "The unit price cannot be negative.")]
    public decimal UnitPrice { get; set; }

    [Range(0, 100, ErrorMessage = "The VAT rate must be between 0 and 100.")]
    public int VatRate { get; set; }

    public int? CompanyId { get; set; }

    /// <summary>Display order on the invoice. Zero means "append to the end".</summary>
    public int OrderNo { get; set; }
}

/// <summary>Invoice line update input.</summary>
public class UpdateInvoiceLineDto : CreateInvoiceLineDto;

/// <summary>Invoice list filter.</summary>
public class GetInvoiceListInput : PagedAndSortedFilterDto
{
    public int? CompanyId { get; set; }
    public int? OfficeId { get; set; }
    public InvoiceType? InvoiceType { get; set; }
    public SourceModule? SourceModule { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>Outstanding invoice balance of a single workplace.</summary>
public class CompanyBalanceDto
{
    public int CompanyId { get; set; }

    /// <summary>Sales invoices minus purchase/return invoices. Positive means the workplace owes money.</summary>
    public decimal Balance { get; set; }

    /// <summary>The instant the balance was calculated.</summary>
    public DateTime CalculatedAt { get; set; }
}

/// <summary>A freshly generated, not-yet-persisted invoice number.</summary>
public class GeneratedInvoiceNumberDto
{
    public string InvoiceNo { get; set; } = string.Empty;
    public int? OfficeId { get; set; }
    public int Year { get; set; }
}
