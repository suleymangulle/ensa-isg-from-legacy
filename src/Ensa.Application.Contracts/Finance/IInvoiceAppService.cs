using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Finance.Dtos;
using Ensa.Application.Contracts.Finance.Dtos.Navigations;

namespace Ensa.Application.Contracts.Finance;

/// <summary>
/// Sales / purchase invoices: header, lines and the derived figures.
/// <para>
/// Every arithmetic concern (line totals, VAT breakdown, grand total, amount in words, invoice
/// number uniqueness and generation) belongs to <c>IInvoiceManager</c>. The service orchestrates
/// and maps; it never recomputes a total itself and never accepts one from the client.
/// </para>
/// </summary>
public interface IInvoiceAppService : IApplicationService
{
    Task<InvoiceDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Detail / print view: header, workplace, office and lines with service-item names.</summary>
    Task<InvoiceNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<InvoiceListDto>> GetListAsync(
        GetInvoiceListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an invoice header. When <c>InvoiceNo</c> is left empty the next number for the
    /// office and year is generated. The header starts with zero totals; they are filled in as
    /// soon as the first line is added.
    /// </summary>
    Task<InvoiceDto> CreateAsync(CreateInvoiceDto input, CancellationToken cancellationToken = default);

    /// <summary>Updates the header. Totals are left untouched — only line changes move them.</summary>
    Task<InvoiceDto> UpdateAsync(int id, UpdateInvoiceDto input, CancellationToken cancellationToken = default);

    /// <summary>Deletes the invoice together with its lines (soft delete).</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ------------------------------------------------------------------ Lines

    /// <summary>Lines of one invoice, in <c>OrderNo</c> order.</summary>
    Task<ListResultDto<InvoiceLineDto>> GetLinesAsync(
        int invoiceId,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a line and recalculates the header totals from the full line set.</summary>
    Task<InvoiceLineDto> AddLineAsync(
        int invoiceId,
        CreateInvoiceLineDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Updates a line and recalculates the header totals from the full line set.</summary>
    Task<InvoiceLineDto> UpdateLineAsync(
        int invoiceId,
        int lineId,
        UpdateInvoiceLineDto input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a line and recalculates the header totals. Removing the last line resets the
    /// header to zero rather than failing, so an invoice can always be emptied and rebuilt.
    /// </summary>
    Task RemoveLineAsync(int invoiceId, int lineId, CancellationToken cancellationToken = default);

    // ------------------------------------------------------------- Derived data

    /// <summary>
    /// Invoice balance of a workplace: sales invoices minus purchase and return invoices.
    /// Computed by the repository in SQL, not by loading invoices into memory.
    /// </summary>
    Task<CompanyBalanceDto> GetCompanyBalanceAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Produces the next invoice number for an office and year without persisting anything.
    /// Useful for pre-filling the create form. The number is validated for uniqueness again at
    /// insert time, so two users opening the form at once cannot create a duplicate.
    /// </summary>
    Task<GeneratedInvoiceNumberDto> GenerateNumberAsync(
        int? officeId,
        int year,
        CancellationToken cancellationToken = default);
}
