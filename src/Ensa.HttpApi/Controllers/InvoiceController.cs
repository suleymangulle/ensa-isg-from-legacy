using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Finance;
using Ensa.Application.Contracts.Finance.Dtos;
using Ensa.Application.Contracts.Finance.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Invoice endpoints — <c>api/invoice</c>.
/// <para>
/// Authorization is enforced by policy; errors are shaped by <c>EnsaExceptionFilter</c>, so there
/// is no <c>try/catch</c> here.
/// </para>
/// </summary>
public class InvoiceController(IInvoiceAppService invoiceAppService) : EnsaController
{
    /// <summary>Returns a single invoice header.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<InvoiceDto> GetAsync(int id, CancellationToken cancellationToken)
        => invoiceAppService.GetAsync(id, cancellationToken);

    /// <summary>Detail / print view: header, workplace, office and lines with service-item names.</summary>
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType<InvoiceNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<InvoiceNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => invoiceAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable invoice list.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<InvoiceListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<InvoiceListDto>> GetListAsync(
        [FromQuery] GetInvoiceListInput input,
        CancellationToken cancellationToken)
        => invoiceAppService.GetListAsync(input, cancellationToken);

    /// <summary>Creates an invoice header. The number is generated when none is supplied.</summary>
    [HttpPost]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<InvoiceDto> CreateAsync(
        [FromBody] CreateInvoiceDto input,
        CancellationToken cancellationToken)
        => invoiceAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates the invoice header. Totals are not touched here.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<InvoiceDto> UpdateAsync(
        int id,
        [FromBody] UpdateInvoiceDto input,
        CancellationToken cancellationToken)
        => invoiceAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the invoice together with its lines (soft delete).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => invoiceAppService.DeleteAsync(id, cancellationToken);

    // ---------------------------------------------------------------- Lines

    /// <summary>Lines of one invoice, in display order.</summary>
    [HttpGet("{id:int}/lines")]
    [ProducesResponseType<ListResultDto<InvoiceLineDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<InvoiceLineDto>> GetLinesAsync(int id, CancellationToken cancellationToken)
        => invoiceAppService.GetLinesAsync(id, cancellationToken);

    /// <summary>Adds a line and recalculates the header totals from the full line set.</summary>
    [HttpPost("{id:int}/lines")]
    [ProducesResponseType<InvoiceLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<InvoiceLineDto> AddLineAsync(
        int id,
        [FromBody] CreateInvoiceLineDto input,
        CancellationToken cancellationToken)
        => invoiceAppService.AddLineAsync(id, input, cancellationToken);

    /// <summary>Updates a line and recalculates the header totals.</summary>
    [HttpPut("{id:int}/lines/{lineId:int}")]
    [ProducesResponseType<InvoiceLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<InvoiceLineDto> UpdateLineAsync(
        int id,
        int lineId,
        [FromBody] UpdateInvoiceLineDto input,
        CancellationToken cancellationToken)
        => invoiceAppService.UpdateLineAsync(id, lineId, input, cancellationToken);

    /// <summary>Removes a line and recalculates the header totals.</summary>
    [HttpDelete("{id:int}/lines/{lineId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RemoveLineAsync(int id, int lineId, CancellationToken cancellationToken)
        => invoiceAppService.RemoveLineAsync(id, lineId, cancellationToken);

    // --------------------------------------------------------- Derived data

    /// <summary>Invoice balance of a workplace.</summary>
    [HttpGet("company/{companyId:int}/balance")]
    [ProducesResponseType<CompanyBalanceDto>(StatusCodes.Status200OK)]
    public Task<CompanyBalanceDto> GetCompanyBalanceAsync(int companyId, CancellationToken cancellationToken)
        => invoiceAppService.GetCompanyBalanceAsync(companyId, cancellationToken);

    /// <summary>Produces the next invoice number for an office and year without persisting it.</summary>
    [HttpGet("next-number")]
    [ProducesResponseType<GeneratedInvoiceNumberDto>(StatusCodes.Status200OK)]
    public Task<GeneratedInvoiceNumberDto> GenerateNumberAsync(
        [FromQuery] int? officeId,
        [FromQuery] int year,
        CancellationToken cancellationToken)
        => invoiceAppService.GenerateNumberAsync(officeId, year, cancellationToken);
}
