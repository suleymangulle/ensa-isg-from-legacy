using Ensa.Domain.Common;
using Ensa.Domain.Reports;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Reports;

/// <summary>
/// EF Core implementation of <see cref="IOhsReportRepository"/>.
/// Tenant filtering comes from the global query filter.
/// </summary>
public class OhsReportRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<OhsReport>(context, dataFilter), IOhsReportRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// The breakdown is read with a <b>single</b> query projecting only the two columns that are needed;
    /// no separate query is issued per hazard class. When a class appears in more than one row, the
    /// counts are summed.
    /// </remarks>
    public async Task<Dictionary<HazardClass, int>> GetHazardClassBreakdownAsync(
        int ohsReportId,
        CancellationToken cancellationToken = default)
    {
        var lines = await Context.Set<OhsReportHazardClassBreakdown>()
            .AsNoTracking()
            .Where(d => d.OhsReportId == ohsReportId)
            .GroupBy(d => d.HazardClass)
            .Select(g => new { HazardClass = g.Key, Count = g.Sum(x => x.CompanyCount) })
            .ToListAsync(cancellationToken);

        return lines.ToDictionary(x => x.HazardClass, x => x.Count);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="OhsReport"/> has no separate period column; the period is filtered on the creation time
    /// of the record (<c>CreationTime</c>). The range is written half-open, without applying any function
    /// to the date column.
    /// </remarks>
    public Task<List<OhsReport>> GetOfficeReportsAsync(
        int officeId,
        DateTime? start = null,
        DateTime? end = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetReadOnlyQueryable().Where(r => r.OfficeId == officeId);

        if (start is { } startValue)
        {
            var lowerBound = startValue.Date;
            query = query.Where(r => r.CreationTime >= lowerBound);
        }

        if (end is { } endValue)
        {
            var upperBound = endValue.Date.AddDays(1);
            query = query.Where(r => r.CreationTime < upperBound);
        }

        return query
            .OrderByDescending(r => r.CreationTime)
            .ThenByDescending(r => r.Id)
            .ToListAsync(cancellationToken);
    }
}
