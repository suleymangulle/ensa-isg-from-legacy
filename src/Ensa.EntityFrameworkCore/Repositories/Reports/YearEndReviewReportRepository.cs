using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Reports;
using Ensa.Domain.Reports.Navigations;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Reports;

/// <summary>
/// EF Core implementation of <see cref="IYearEndReviewReportRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class YearEndReviewReportRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<YearEndReviewReport>(context, dataFilter),
      IYearEndReviewReportRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// <b>N+1 PREVENTION:</b> the hierarchy is not built with a recursive query but by fetching <b>all</b>
    /// lines of the report in a single query and grouping them in memory by <c>ParentLineId</c>. Whatever
    /// the depth of the tree, the total query count is 3 (report + company + lines).
    /// <para>
    /// Visited lines are tracked so that corrupt data with a cyclic <c>ParentLineId</c> reference does not
    /// cause infinite recursion.
    /// </para>
    /// </remarks>
    public async Task<YearEndReviewReportNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var report = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report is null)
        {
            return null;
        }

        var navigation = new YearEndReviewReportNavigation
        {
            YearEndReviewReport = report,

            Company = await Context.Set<Company>()
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == report.CompanyId, cancellationToken)
        };

        var lines = await Context.Set<YearEndReviewLine>()
            .AsNoTracking()
            .Where(s => s.YearEndReviewReportId == id)
            .OrderBy(s => s.OrderNo)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        var childGroups = lines
            .Where(s => s.ParentLineId.HasValue)
            .GroupBy(s => s.ParentLineId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var visit = new HashSet<int>();

        navigation.Activities = lines
            .Where(s => s.ParentLineId is null)
            .Select(s => BuildTree(s, childGroups, visit))
            .ToList();

        return navigation;
    }

    /// <inheritdoc />
    public Task<YearEndReviewReport?> GetCurrentReportAsync(
        int companyId,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .Where(r => r.CompanyId == companyId && r.IsActive)
            .OrderByDescending(r => r.ReportDate)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Builds the child work tree from the in-memory line dictionary.</summary>
    private static YearEndReviewLineNavigation BuildTree(
        YearEndReviewLine line,
        Dictionary<int, List<YearEndReviewLine>> childGroups,
        HashSet<int> visit)
    {
        var node = new YearEndReviewLineNavigation { Line = line };

        // Descending into the same row twice means the data is cyclic; leave the subtree empty and bail out.
        if (!visit.Add(line.Id) || !childGroups.TryGetValue(line.Id, out var children))
        {
            return node;
        }

        node.ChildActivities = children.ConvertAll(child => BuildTree(child, childGroups, visit));

        return node;
    }
}
