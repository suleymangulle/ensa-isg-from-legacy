using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Finance.Dtos;
using Ensa.Application.Contracts.Finance.Dtos.Navigations;

namespace Ensa.Application.Contracts.Finance;

/// <summary>
/// Cash registers and their movements.
/// <para>
/// <b>Movements are an append-only ledger.</b> There is deliberately no update and no delete for
/// a <c>CashTransaction</c>: a cash balance is a financial figure that auditors reconstruct by
/// replaying the movements, so rewriting or erasing history would make past balances
/// unreproducible and would silently invalidate every report already issued. A mistaken movement
/// is therefore corrected by voiding it — <see cref="VoidTransactionAsync"/> flips
/// <c>IsActive</c> to <c>false</c>, which removes it from balance arithmetic while leaving the
/// row, its author and its timestamps intact — and, where a correcting entry is wanted, by
/// posting a new opposite movement.
/// </para>
/// </summary>
public interface ICashRegisterAppService : IApplicationService
{
    Task<CashRegisterDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Detail view: register, office, current balance and the latest movements.</summary>
    Task<CashRegisterNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<CashRegisterListDto>> GetListAsync(
        GetCashRegisterListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Lightweight records for drop-down lists.</summary>
    Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default);

    Task<CashRegisterDto> CreateAsync(CreateCashRegisterDto input, CancellationToken cancellationToken = default);

    Task<CashRegisterDto> UpdateAsync(
        int id,
        UpdateCashRegisterDto input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the register (soft delete). Refused while the register still holds a non-zero
    /// balance, because deleting it would make that money disappear from the books.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ---------------------------------------------------------------- Balance

    /// <summary>
    /// Balance of a register: total of entries minus total of exits, voided movements excluded.
    /// </summary>
    /// <param name="cashRegisterId">The register.</param>
    /// <param name="asOf">Cut-off instant. When omitted the balance is taken as of now.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<CashRegisterBalanceDto> GetBalanceAsync(
        int cashRegisterId,
        DateTime? asOf = null,
        CancellationToken cancellationToken = default);

    // ----------------------------------------------------------- Transactions

    Task<PagedResultDto<CashTransactionDto>> GetTransactionListAsync(
        GetCashTransactionListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a movement to the ledger. An exit that would drive the register's balance below
    /// zero is refused, since a physical cash box cannot hold a negative amount.
    /// </summary>
    Task<CashTransactionDto> AddTransactionAsync(
        CreateCashTransactionDto input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Voids a movement by setting <c>IsActive = false</c>. The row is kept: see the append-only
    /// rationale on this interface. Voiding an entry that would leave the register negative is
    /// refused for the same reason an over-drawing exit is.
    /// </summary>
    Task<CashTransactionDto> VoidTransactionAsync(
        int transactionId,
        CancellationToken cancellationToken = default);
}
