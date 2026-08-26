using Ensa.Domain.Reports.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Reports;

/// <summary>
/// Module-specific queries for <see cref="YearEndReviewReport"/>.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IYearEndReviewReportRepository : IRepository<YearEndReviewReport>
{
    /// <summary>
    /// Loads the report together with its company details and the hierarchical tree of activity lines
    /// (<see cref="YearEndReviewLineNavigation.ChildActivities"/>).
    /// </summary>
    Task<YearEndReviewReportNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a company's most recent year-end review report.</summary>
    Task<YearEndReviewReport?> GetCurrentReportAsync(
        int companyId,
        CancellationToken cancellationToken = default);
}
