using Ensa.Domain.Common;
using Ensa.Domain.Finance;
using Ensa.Domain.Finance.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Finance;

/// <summary>
/// EF Core implementation of <see cref="IPenaltyRepository"/>.
/// <para>
/// <see cref="Penalty"/> and <see cref="PenaltyAmount"/> are host (tenant-less) reference tables; only the
/// soft-delete global filter applies to them.
/// </para>
/// </summary>
public class PenaltyRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<Penalty>(context, dataFilter), IPenaltyRepository
{
    /// <inheritdoc />
    /// <remarks>The amount matrix is fetched with a single query; no extra query is issued per row (2 queries).</remarks>
    public async Task<PenaltyNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var penalty = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (penalty is null)
        {
            return null;
        }

        return new PenaltyNavigation
        {
            Penalty = penalty,
            Amounts = await Context.Set<PenaltyAmount>()
                .AsNoTracking()
                .Where(t => t.PenaltyId == id)
                .OrderByDescending(t => t.ValidityYear)
                .ThenBy(t => t.HazardClass)
                .ThenBy(t => t.EmployeeCountRange)
                .ToListAsync(cancellationToken)
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// When there is no record for the requested year, the most recent record satisfying
    /// <c>ValidityYear &lt;= year</c> is used (the previous year's amount stays in force until the new
    /// year's amount is published). The selection happens <b>in the database</b> with <c>TOP 1</c>.
    /// </remarks>
    public Task<decimal?> GetAmountAsync(
        int penaltyId,
        HazardClass hazardClass,
        EmployeeCountRange employeeCountRange,
        int year,
        CancellationToken cancellationToken = default)
        => Context.Set<PenaltyAmount>()
            .AsNoTracking()
            .Where(t => t.PenaltyId == penaltyId
                        && t.HazardClass == hazardClass
                        && t.EmployeeCountRange == employeeCountRange
                        && t.ValidityYear <= year)
            .OrderByDescending(t => t.ValidityYear)
            .Select(t => (decimal?)t.Amount)
            .FirstOrDefaultAsync(cancellationToken);
}
