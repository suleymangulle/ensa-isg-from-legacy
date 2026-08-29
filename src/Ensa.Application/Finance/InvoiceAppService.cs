using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Finance;
using Ensa.Application.Contracts.Finance.Dtos;
using Ensa.Application.Contracts.Finance.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Finance;
using Ensa.Domain.Tenancy;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Finance;

/// <summary>
/// Invoice application service.
/// <para>
/// All arithmetic lives in <see cref="IInvoiceManager"/>. Note that this manager is a pure
/// calculation and validation service: unlike <c>CompanyManager</c> it does <b>not</b> persist
/// anything, so this service is responsible for its own inserts and updates.
/// </para>
/// <para>
/// Header totals are never read from the client. Every path that touches a line recomputes them
/// from the complete line set, so a tampered or stale payload cannot leave an invoice whose
/// header disagrees with its own lines.
/// </para>
/// </summary>
public class InvoiceAppService(
    IServiceProvider serviceProvider,
    IInvoiceRepository invoiceRepository,
    IRepository<InvoiceLine> invoiceLineRepository,
    IInvoiceManager invoiceManager)
    : EnsaAppService(serviceProvider), IInvoiceAppService
{
    /// <inheritdoc />
    public async Task<InvoiceDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Invoice.Default);

        var invoice = await invoiceRepository.FindAsync(id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Invoice), id);

        return ObjectMapper.Map<Invoice, InvoiceDto>(invoice);
    }

    /// <inheritdoc />
    public async Task<InvoiceNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Invoice.Default);

        var navigation = await invoiceRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(Invoice), id);

        return new InvoiceNavigationDto
        {
            Invoice = ObjectMapper.Map<Invoice, InvoiceDto>(navigation.Invoice),
            Company = navigation.Company is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Company.Id,
                    DisplayName = navigation.Company.CompanyName,
                    Code = navigation.Company.SsiNumber,
                    IsActive = navigation.Company.IsActive
                },
            Office = navigation.Office is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Office.Id,
                    DisplayName = navigation.Office.Name,
                    IsActive = navigation.Office.IsActive
                },
            Lines =
            [
                .. navigation.Lines
                    .OrderBy(l => l.InvoiceLine.OrderNo)
                    .Select(l => new InvoiceLineNavigationDto
                    {
                        Line = ObjectMapper.Map<InvoiceLine, InvoiceLineDto>(l.InvoiceLine),
                        ServiceItem = l.ServiceItem is null
                            ? null
                            : new LookupDto
                            {
                                Id = l.ServiceItem.Id,
                                DisplayName = l.ServiceItem.Name,
                                Code = l.ServiceItem.Code,
                                IsActive = l.ServiceItem.IsActive
                            }
                    })
            ]
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<InvoiceListDto>> GetListAsync(
        GetInvoiceListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Invoice.Default);

        var predicate = BuildFilter(input, ResolveOfficeScope(input.OfficeId));
        var sorting = NormalizeSorting(input.Sorting, "InvoiceDate DESC");

        var total = await invoiceRepository.GetCountAsync(predicate, cancellationToken);

        var records = await invoiceRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Invoice>, List<InvoiceListDto>>(records);

        return new PagedResultDto<InvoiceListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<InvoiceDto> CreateAsync(
        CreateInvoiceDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Invoice.Create);

        var invoice = ObjectMapper.Map<CreateInvoiceDto, Invoice>(input);

        // An invoice is filed against an office, and the office the user is working in is the one
        // it belongs to. A body that names a different office is refused by ResolveOfficeScope
        // rather than quietly overridden; a body that names none inherits the working office, which
        // is what stops an invoice created while looking at one office from landing in another.
        invoice.OfficeId = ResolveWriteOfficeId(input.OfficeId);

        invoice.InvoiceNo = string.IsNullOrWhiteSpace(input.InvoiceNo)
            ? await invoiceManager.GenerateInvoiceNumberAsync(
                invoice.OfficeId,
                input.InvoiceDate.Year,
                cancellationToken)
            : input.InvoiceNo.Trim();

        await invoiceManager.ValidateInvoiceNoUniqueAsync(
            invoice.InvoiceNo,
            exceptInvoiceId: null,
            cancellationToken);

        // A brand-new header has no lines yet, so its totals are zero. They are filled in by
        // RecalculateTotalsAsync as soon as the first line arrives; CalculateInvoiceTotals is not
        // called here because it (correctly) refuses an empty line set.
        invoice.Total = 0m;
        invoice.VatTotal = 0m;
        invoice.GeneralTotal = 0m;

        // IInvoiceManager does not persist — unlike CompanyManager — so the insert is ours.
        invoice = await invoiceRepository.InsertAsync(invoice, autoSave: true, cancellationToken);

        Logger.LogInformation("Invoice created: {InvoiceId} — {InvoiceNo}", invoice.Id, invoice.InvoiceNo);

        return ObjectMapper.Map<Invoice, InvoiceDto>(invoice);
    }

    /// <inheritdoc />
    public async Task<InvoiceDto> UpdateAsync(
        int id,
        UpdateInvoiceDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Invoice.Update);

        var invoice = await invoiceRepository.FindAsync(id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Invoice), id);

        var requestedNo = string.IsNullOrWhiteSpace(input.InvoiceNo)
            ? invoice.InvoiceNo
            : input.InvoiceNo.Trim();

        if (!string.Equals(requestedNo, invoice.InvoiceNo, StringComparison.Ordinal))
        {
            await invoiceManager.ValidateInvoiceNoUniqueAsync(requestedNo, id, cancellationToken);
        }

        var previousTotal = invoice.Total;
        var previousVatTotal = invoice.VatTotal;
        var previousGeneralTotal = invoice.GeneralTotal;
        var previousInWords = invoice.InWords;

        ObjectMapper.Map(input, invoice);

        // The mapping only carries header fields, but restoring the derived values explicitly
        // documents that a header edit never moves a total — only a line change does.
        invoice.InvoiceNo = requestedNo;
        invoice.Total = previousTotal;
        invoice.VatTotal = previousVatTotal;
        invoice.GeneralTotal = previousGeneralTotal;
        invoice.InWords = previousInWords;

        invoice = await invoiceRepository.UpdateAsync(invoice, autoSave: true, cancellationToken);

        return ObjectMapper.Map<Invoice, InvoiceDto>(invoice);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Invoice.Delete);

        var invoice = await invoiceRepository.FindAsync(id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Invoice), id);

        var lines = await invoiceRepository.GetLinesAsync(id, cancellationToken);
        if (lines.Count > 0)
        {
            await invoiceLineRepository.DeleteManyAsync(lines, autoSave: false, cancellationToken);
        }

        await invoiceRepository.DeleteAsync(invoice, autoSave: true, cancellationToken);

        Logger.LogInformation("Invoice deleted: {InvoiceId} ({LineCount} lines)", id, lines.Count);
    }

    // ---------------------------------------------------------------- Lines

    /// <inheritdoc />
    public async Task<ListResultDto<InvoiceLineDto>> GetLinesAsync(
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Invoice.Default);

        _ = await invoiceRepository.FindAsync(invoiceId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Invoice), invoiceId);

        var lines = await invoiceRepository.GetLinesAsync(invoiceId, cancellationToken);

        return new ListResultDto<InvoiceLineDto>(
            ObjectMapper.Map<List<InvoiceLine>, List<InvoiceLineDto>>(lines));
    }

    /// <inheritdoc />
    public async Task<InvoiceLineDto> AddLineAsync(
        int invoiceId,
        CreateInvoiceLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Invoice.Update);

        var invoice = await invoiceRepository.FindAsync(invoiceId, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Invoice), invoiceId);

        var line = ObjectMapper.Map<CreateInvoiceLineDto, InvoiceLine>(input);
        line.InvoiceId = invoiceId;

        if (line.OrderNo <= 0)
        {
            var existing = await invoiceRepository.GetLinesAsync(invoiceId, cancellationToken);
            line.OrderNo = existing.Count == 0 ? 1 : existing.Max(l => l.OrderNo) + 1;
        }

        // The manager owns the line arithmetic; the service never multiplies a price itself.
        invoiceManager.CalculateLineTotals(line);

        line = await invoiceLineRepository.InsertAsync(line, autoSave: true, cancellationToken);

        await RecalculateTotalsAsync(invoice, cancellationToken);

        return ObjectMapper.Map<InvoiceLine, InvoiceLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<InvoiceLineDto> UpdateLineAsync(
        int invoiceId,
        int lineId,
        UpdateInvoiceLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Invoice.Update);

        var invoice = await invoiceRepository.FindAsync(invoiceId, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Invoice), invoiceId);

        var line = await GetLineOfInvoiceAsync(invoice, lineId, cancellationToken);

        var orderNo = line.OrderNo;
        ObjectMapper.Map(input, line);
        line.InvoiceId = invoiceId;
        line.OrderNo = input.OrderNo > 0 ? input.OrderNo : orderNo;

        invoiceManager.CalculateLineTotals(line);

        line = await invoiceLineRepository.UpdateAsync(line, autoSave: true, cancellationToken);

        await RecalculateTotalsAsync(invoice, cancellationToken);

        return ObjectMapper.Map<InvoiceLine, InvoiceLineDto>(line);
    }

    /// <inheritdoc />
    public async Task RemoveLineAsync(
        int invoiceId,
        int lineId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Invoice.Update);

        var invoice = await invoiceRepository.FindAsync(invoiceId, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Invoice), invoiceId);

        var line = await GetLineOfInvoiceAsync(invoice, lineId, cancellationToken);

        await invoiceLineRepository.DeleteAsync(line, autoSave: true, cancellationToken);

        await RecalculateTotalsAsync(invoice, cancellationToken);
    }

    // --------------------------------------------------------- Derived data

    /// <inheritdoc />
    public async Task<CompanyBalanceDto> GetCompanyBalanceAsync(
        int companyId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Invoice.Default);

        var now = Clock.Now;

        var balance = await invoiceRepository.GetCompanyBalanceAsync(companyId, now, cancellationToken);

        return new CompanyBalanceDto
        {
            CompanyId = companyId,
            Balance = balance,
            CalculatedAt = now
        };
    }

    /// <inheritdoc />
    public async Task<GeneratedInvoiceNumberDto> GenerateNumberAsync(
        int? officeId,
        int year,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Invoice.Create);
        ValidateCalendarYear(year);

        // Invoice numbers run per office and year (ADR-017), so the number this hands out has to be
        // drawn from the same office the invoice will be filed against.
        var effectiveOfficeId = ResolveWriteOfficeId(officeId);

        var invoiceNo = await invoiceManager.GenerateInvoiceNumberAsync(
            effectiveOfficeId, year, cancellationToken);

        return new GeneratedInvoiceNumberDto
        {
            InvoiceNo = invoiceNo,
            OfficeId = effectiveOfficeId,
            Year = year
        };
    }

    // -----------------------------------------------------------------

    /// <summary>
    /// Rebuilds the header figures from the current line set and persists both.
    /// <para>
    /// An empty line set resets the header to zero instead of failing:
    /// <c>CalculateInvoiceTotals</c> rejects an empty collection, which is the right rule when
    /// finalising an invoice but the wrong one while a user is emptying it to start over.
    /// </para>
    /// </summary>
    private async Task RecalculateTotalsAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var lines = await invoiceRepository.GetLinesAsync(invoice.Id, cancellationToken);

        if (lines.Count == 0)
        {
            invoice.Total = 0m;
            invoice.VatTotal = 0m;
            invoice.GeneralTotal = 0m;
            invoice.InWords = null;
        }
        else
        {
            // CalculateInvoiceTotals also refreshes every line's own totals, so the lines are
            // written back alongside the header.
            invoiceManager.CalculateInvoiceTotals(invoice, lines);
            await invoiceLineRepository.UpdateManyAsync(lines, autoSave: false, cancellationToken);
        }

        await invoiceRepository.UpdateAsync(invoice, autoSave: true, cancellationToken);
    }

    private async Task<InvoiceLine> GetLineOfInvoiceAsync(
        Invoice invoice,
        int lineId,
        CancellationToken cancellationToken)
    {
        var line = await invoiceLineRepository.FindAsync(lineId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(InvoiceLine), lineId);

        if (line.InvoiceId != invoice.Id)
        {
            throw new BusinessException(
                    "The line does not belong to this invoice.",
                    "Ensa:Invoice:LineNotInInvoice")
                .WithData("InvoiceNo", invoice.InvoiceNo);
        }

        return line;
    }

    /// <summary>
    /// The office a write should be filed against: the one the caller named if it does not
    /// contradict the office the request is running for, otherwise the working office.
    /// <para>
    /// <c>null</c> when there is no single answer — no office context and no value in the body, or
    /// an "all offices" scope spanning several. That is the behaviour this endpoint already had, and
    /// <c>Invoice.OfficeId</c> is nullable precisely because an invoice need not name one.
    /// </para>
    /// </summary>
    private int? ResolveWriteOfficeId(int? requestedOfficeId)
    {
        var scope = ResolveOfficeScope(requestedOfficeId);
        return requestedOfficeId ?? scope.SingleOfficeId;
    }

    /// <summary>
    /// The invoice list filter.
    /// <para>
    /// <paramref name="officeScope"/> already reconciled <c>input.OfficeId</c> with the office the
    /// request is running for, so the caller-supplied value is not read again here. An invoice with
    /// no office of its own falls outside a restricted scope, which is the same answer the legacy
    /// <c>f.OfisId == OfisId</c> join gave.
    /// </para>
    /// </summary>
    private static Expression<Func<Invoice, bool>>? BuildFilter(
        GetInvoiceListInput input,
        OfficeQueryScope officeScope)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var companyId = input.CompanyId;
        var officeIds = officeScope.OfficeIds;
        var restricted = officeScope.IsRestricted;
        var invoiceType = input.InvoiceType;
        var sourceModule = input.SourceModule;
        var startDate = input.StartDate;
        var endDate = input.EndDate;

        if (search is null
            && companyId is null
            && !restricted
            && invoiceType is null
            && sourceModule is null
            && startDate is null
            && endDate is null)
        {
            return null;
        }

        return f =>
            (search == null || f.InvoiceNo.Contains(search) || f.AccountCurrentName.Contains(search))
            && (companyId == null || f.CompanyId == companyId)
            && (!restricted || (f.OfficeId != null && officeIds.Contains(f.OfficeId.Value)))
            && (invoiceType == null || f.InvoiceType == invoiceType)
            && (sourceModule == null || f.SourceModule == sourceModule)
            && (startDate == null || f.InvoiceDate >= startDate)
            && (endDate == null || f.InvoiceDate <= endDate);
    }
}
