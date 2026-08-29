using Ensa.Domain.Common;
using Ensa.Domain.Finance;
using Ensa.Domain.Finance.Navigations;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Finance;

/// <summary>
/// EF Core implementation of <see cref="ICashRegisterRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class CashRegisterRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<CashRegister>(context, dataFilter), ICashRegisterRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// The recent transactions are capped in the database with <c>Take</c>; the full transaction history
    /// is never loaded into memory. The total query count is constant (register, office, balance, recent
    /// transactions = 4).
    /// </remarks>
    public async Task<CashRegisterNavigation?> GetWithNavigationAsync(
        int id,
        int latestTransactionCount = 20,
        CancellationToken cancellationToken = default)
    {
        var cashRegister = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

        if (cashRegister is null)
        {
            return null;
        }

        var navigation = new CashRegisterNavigation { CashRegister = cashRegister };

        navigation.Office = await Context.Set<Office>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == cashRegister.OfficeId, cancellationToken);

        navigation.Balance = await GetBalanceAsync(id, date: null, cancellationToken);

        var takeCount = Math.Clamp(latestTransactionCount, 1, 500);

        navigation.LatestTransactions = await Context.Set<CashTransaction>()
            .AsNoTracking()
            .Where(h => h.CashRegisterId == id && h.IsActive)
            .OrderByDescending(h => h.OperationDate ?? h.CreationTime)
            .ThenByDescending(h => h.Id)
            .Take(takeCount)
            .ToListAsync(cancellationToken);

        return navigation;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The balance is computed <b>in the database</b> with a single <c>SUM(CASE ...)</c> query; the
    /// transactions are not loaded into memory to be summed. <see cref="CashTransactionType.Outflow"/>
    /// counts as negative and the others (<see cref="CashTransactionType.Inflow"/> and
    /// <see cref="CashTransactionType.CarryOver"/> — the opening amount carried over from the previous
    /// period) as positive. For transactions with no transaction date the creation time is used.
    /// </remarks>
    public async Task<decimal> GetBalanceAsync(
        int cashRegisterId,
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var upperBound = (date ?? DateTime.Now).Date.AddDays(1);

        return await Context.Set<CashTransaction>()
            .AsNoTracking()
            .Where(h => h.CashRegisterId == cashRegisterId
                        && h.IsActive
                        && (h.OperationDate ?? h.CreationTime) < upperBound)
            .SumAsync(
                h => h.OperationType == CashTransactionType.Outflow ? -h.OperationAmount : h.OperationAmount,
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<CashRegister?> GetHeadquarterCashRegisterAsync(
        int officeId,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .Where(k => k.OfficeId == officeId && k.IsHeadquarterCashRegister && k.IsActive)
            .OrderBy(k => k.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
