using Ensa.Domain.Common;
using Ensa.Domain.Finance;
using Ensa.Domain.Finance.Navigations;
using Ensa.Domain.Companies;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Finance;

/// <summary>
/// EF Core implementation of <see cref="IInvoiceRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class InvoiceRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<Invoice>(context, dataFilter), IInvoiceRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// <b>N+1 PREVENTION:</b> the service items of the lines are fetched in a single query with
    /// <c>Contains</c> rather than per line, and matched up in memory. The total query count is at most 5
    /// regardless of the number of lines.
    /// </remarks>
    public async Task<InvoiceNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var invoice = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        var navigation = new InvoiceNavigation { Invoice = invoice };

        navigation.Company = await Context.Set<Company>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == invoice.CompanyId, cancellationToken);

        if (invoice.OfficeId is { } officeId)
        {
            navigation.Office = await Context.Set<Office>()
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == officeId, cancellationToken);
        }

        var lines = await Context.Set<InvoiceLine>()
            .AsNoTracking()
            .Where(s => s.InvoiceId == id)
            .OrderBy(s => s.OrderNo)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        var serviceItemIds = lines
            .Where(s => s.ServiceItemId.HasValue)
            .Select(s => s.ServiceItemId!.Value)
            .Distinct()
            .ToList();

        List<ServiceItem> serviceCards = serviceItemIds.Count == 0
            ? []
            : await Context.Set<ServiceItem>()
                .AsNoTracking()
                .Where(h => serviceItemIds.Contains(h.Id))
                .ToListAsync(cancellationToken);

        navigation.Lines = lines.ConvertAll(line => new InvoiceLineNavigation
        {
            InvoiceLine = line,
            ServiceItem = serviceCards.Find(h => h.Id == line.ServiceItemId)
        });

        return navigation;
    }

    /// <inheritdoc />
    public Task<bool> InvoiceNumberExistsAsync(
        string invoiceNo,
        int? exceptInvoiceId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNo))
        {
            return Task.FromResult(false);
        }

        var value = invoiceNo.Trim();

        return GetReadOnlyQueryable()
            .AnyAsync(
                f => f.InvoiceNo == value && (exceptInvoiceId == null || f.Id != exceptInvoiceId),
                cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The balance is computed <b>in the database</b> with a single <c>SUM(CASE ...)</c> query; the
    /// invoices are not loaded into memory to be summed. Sales invoices count as positive, purchase and
    /// refund invoices as negative.
    /// </remarks>
    public async Task<decimal> GetCompanyBalanceAsync(
        int companyId,
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var upperBound = (date ?? DateTime.Now).Date.AddDays(1);

        return await GetReadOnlyQueryable()
            .Where(f => f.CompanyId == companyId && f.InvoiceDate < upperBound)
            .SumAsync(
                f => f.InvoiceType == InvoiceType.Sale ? f.GeneralTotal : -f.GeneralTotal,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<InvoiceLine>> GetLinesAsync(
        int invoiceId,
        CancellationToken cancellationToken = default)
        => Context.Set<InvoiceLine>()
            .AsNoTracking()
            .Where(s => s.InvoiceId == invoiceId)
            .OrderBy(s => s.OrderNo)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);
}
