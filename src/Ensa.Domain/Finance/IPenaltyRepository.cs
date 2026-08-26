using Ensa.Domain.Finance.Navigations;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Finance;

/// <summary>
/// Module-specific queries for <see cref="Penalty"/>.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IPenaltyRepository : IRepository<Penalty>
{
    /// <summary>Loads the penalty as a combined view with its amount matrix (<see cref="PenaltyAmount"/>).</summary>
    Task<PenaltyNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the penalty amount in force for the given year, for a workplace hazard class and
    /// employee count. When there is no record for <paramref name="year"/>, the amount from the
    /// nearest preceding year is used.
    /// </summary>
    Task<decimal?> GetAmountAsync(
        int penaltyId,
        HazardClass hazardClass,
        EmployeeCountRange employeeCountRange,
        int year,
        CancellationToken cancellationToken = default);
}
