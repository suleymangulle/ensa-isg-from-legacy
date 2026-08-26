using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Finance;
using Ensa.Application.Contracts.Finance.Dtos;
using Ensa.Application.Contracts.Finance.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Cash register endpoints — <c>api/cash-register</c>.
/// <para>
/// Note the shape of the transaction routes: there is a POST to append a movement and a POST to
/// void one, but no PUT and no DELETE. Movements are an append-only ledger — see
/// <see cref="ICashRegisterAppService"/> for why.
/// </para>
/// </summary>
public class CashRegisterController(ICashRegisterAppService cashRegisterAppService) : EnsaController
{
    /// <summary>Returns a single cash register.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.CashRegister.Default)]
    [ProducesResponseType<CashRegisterDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CashRegisterDto> GetAsync(int id, CancellationToken cancellationToken)
        => cashRegisterAppService.GetAsync(id, cancellationToken);

    /// <summary>Detail view: register, office, current balance and the latest movements.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.CashRegister.Default)]
    [ProducesResponseType<CashRegisterNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CashRegisterNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => cashRegisterAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable cash register list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.CashRegister.Default)]
    [ProducesResponseType<PagedResultDto<CashRegisterListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<CashRegisterListDto>> GetListAsync(
        [FromQuery] GetCashRegisterListInput input,
        CancellationToken cancellationToken)
        => cashRegisterAppService.GetListAsync(input, cancellationToken);

    /// <summary>Lightweight records for drop-down lists (at most 50).</summary>
    [HttpGet("lookup")]
    [Authorize(EnsaPermissions.CashRegister.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetLookupAsync(
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
        => cashRegisterAppService.GetLookupAsync(filter, cancellationToken);

    /// <summary>Creates a cash register.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.CashRegister.Create)]
    [ProducesResponseType<CashRegisterDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<CashRegisterDto> CreateAsync(
        [FromBody] CreateCashRegisterDto input,
        CancellationToken cancellationToken)
        => cashRegisterAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates a cash register.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.CashRegister.Update)]
    [ProducesResponseType<CashRegisterDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CashRegisterDto> UpdateAsync(
        int id,
        [FromBody] UpdateCashRegisterDto input,
        CancellationToken cancellationToken)
        => cashRegisterAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes a cash register. Refused while it still holds a balance.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.CashRegister.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => cashRegisterAppService.DeleteAsync(id, cancellationToken);

    // --------------------------------------------------------------- Balance

    /// <summary>Balance of a register, optionally as of a past instant.</summary>
    [HttpGet("{id:int}/balance")]
    [Authorize(EnsaPermissions.CashRegister.Default)]
    [ProducesResponseType<CashRegisterBalanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CashRegisterBalanceDto> GetBalanceAsync(
        int id,
        [FromQuery] DateTime? asOf,
        CancellationToken cancellationToken)
        => cashRegisterAppService.GetBalanceAsync(id, asOf, cancellationToken);

    // ---------------------------------------------------------- Transactions

    /// <summary>Paged movement list of one register.</summary>
    [HttpGet("transactions")]
    [Authorize(EnsaPermissions.CashRegister.Default)]
    [ProducesResponseType<PagedResultDto<CashTransactionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PagedResultDto<CashTransactionDto>> GetTransactionListAsync(
        [FromQuery] GetCashTransactionListInput input,
        CancellationToken cancellationToken)
        => cashRegisterAppService.GetTransactionListAsync(input, cancellationToken);

    /// <summary>Appends a movement to the ledger.</summary>
    [HttpPost("transactions")]
    [Authorize(EnsaPermissions.CashRegister.Create)]
    [ProducesResponseType<CashTransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<CashTransactionDto> AddTransactionAsync(
        [FromBody] CreateCashTransactionDto input,
        CancellationToken cancellationToken)
        => cashRegisterAppService.AddTransactionAsync(input, cancellationToken);

    /// <summary>
    /// Voids a movement. This is the only way to undo one: the ledger is append-only, so the row
    /// stays and simply stops counting towards the balance.
    /// </summary>
    [HttpPost("transactions/{transactionId:int}/void")]
    [Authorize(EnsaPermissions.CashRegister.Update)]
    [ProducesResponseType<CashTransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CashTransactionDto> VoidTransactionAsync(int transactionId, CancellationToken cancellationToken)
        => cashRegisterAppService.VoidTransactionAsync(transactionId, cancellationToken);
}
