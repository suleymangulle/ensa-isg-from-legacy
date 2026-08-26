using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Reports;
using Ensa.Domain.Reports.Navigations;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Reports;

/// <summary>
/// EF Core implementation of <see cref="IActivityReportRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class ActivityReportRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<ActivityReport>(context, dataFilter), IActivityReportRepository
{
    /// <inheritdoc />
    /// <remarks>The data rows are fetched with a single query; no extra query is issued per row (3 queries).</remarks>
    public async Task<ActivityReportNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var report = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report is null)
        {
            return null;
        }

        return new ActivityReportNavigation
        {
            ActivityReport = report,

            Company = await Context.Set<Company>()
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == report.CompanyId, cancellationToken),

            Lines = await Context.Set<ActivityReportLine>()
                .AsNoTracking()
                .Where(s => s.ActivityReportId == id)
                .OrderBy(s => s.OrderNo)
                .ThenBy(s => s.Id)
                .ToListAsync(cancellationToken)
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// The range condition is written as a half-open interval without applying a function to the date
    /// column, so that the <c>(CompanyId, ReportStart)</c> index can be used.
    /// </remarks>
    public Task<List<ActivityReport>> GetCompanyReportsAsync(
        int companyId,
        DateTime? start = null,
        DateTime? end = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetReadOnlyQueryable().Where(r => r.CompanyId == companyId);

        if (start is { } startValue)
        {
            var lowerBound = startValue.Date;
            query = query.Where(r => r.ReportEnd >= lowerBound);
        }

        if (end is { } endValue)
        {
            var upperBound = endValue.Date.AddDays(1);
            query = query.Where(r => r.ReportStart < upperBound);
        }

        return query
            .OrderByDescending(r => r.ReportStart)
            .ThenByDescending(r => r.Id)
            .ToListAsync(cancellationToken);
    }
}
