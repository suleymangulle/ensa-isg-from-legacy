using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Tenancy;

namespace Ensa.Domain.Finance.Navigations;

/// <summary>
/// Combined view of a <see cref="CashRegister"/> with its office, current balance and latest
/// movements.
/// <para>
/// RULE: it is <c>[NotMapped]</c>, never a <c>DbSet</c>, and never added to <c>ModelBuilder</c>.
/// <c>ICashRegisterRepository.GetWithNavigationAsync</c> populates it through an <c>IQueryable</c>
/// join and projection; <see cref="Balance"/> comes from
/// <c>ICashRegisterRepository.GetBalanceAsync</c>.
/// </para>
/// </summary>
[NotMapped]
public class CashRegisterNavigation : NavigationEntity
{
    /// <summary>The mapped root entity.</summary>
    public CashRegister CashRegister { get; set; } = null!;

    public Office? Office { get; set; }

    /// <summary>Cash register balance at query time (total in minus total out).</summary>
    public decimal Balance { get; set; }

    /// <summary>The last N movements, for the default screen listing.</summary>
    public List<CashTransaction> LatestTransactions { get; set; } = [];
}
