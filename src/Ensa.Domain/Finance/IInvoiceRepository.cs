using Ensa.Domain.Finance.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Finance;

/// <summary>
/// Module-specific queries for <see cref="Invoice"/>.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IInvoiceRepository : IRepository<Invoice>
{
    /// <summary>Loads the invoice as a combined view with its company, office and lines, including service item names.</summary>
    Task<InvoiceNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>Returns whether the given invoice number is already in use within the active tenant.</summary>
    Task<bool> InvoiceNumberExistsAsync(
        string invoiceNo,
        int? exceptInvoiceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a company's invoice balance up to the given date — now by default — as the total of
    /// sales invoices minus the total of purchase and refund invoices.
    /// </summary>
    Task<decimal> GetCompanyBalanceAsync(
        int companyId,
        DateTime? date = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns an invoice's lines, ordered by line number.</summary>
    Task<List<InvoiceLine>> GetLinesAsync(
        int invoiceId,
        CancellationToken cancellationToken = default);
}
