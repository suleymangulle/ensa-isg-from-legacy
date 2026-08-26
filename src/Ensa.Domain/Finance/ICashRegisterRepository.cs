using Ensa.Domain.Finance.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Finance;

/// <summary>
/// Module-specific queries for <see cref="CashRegister"/>.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface ICashRegisterRepository : IRepository<CashRegister>
{
    /// <summary>Loads the cash register as a combined view with its office, balance and latest movements.</summary>
    Task<CashRegisterNavigation?> GetWithNavigationAsync(
        int id,
        int latestTransactionCount = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the cash register's balance — total in minus total out — as of
    /// <paramref name="date"/>, or as of now when it is omitted.
    /// </summary>
    Task<decimal> GetBalanceAsync(
        int cashRegisterId,
        DateTime? date = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns an office's headquarter cash register, if it has one.</summary>
    Task<CashRegister?> GetHeadquarterCashRegisterAsync(
        int officeId,
        CancellationToken cancellationToken = default);
}
