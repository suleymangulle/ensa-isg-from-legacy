using Ensa.Domain.Reports.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Reports;

/// <summary>
/// Module-specific queries for <see cref="ActivityReport"/>.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IActivityReportRepository : IRepository<ActivityReport>
{
    /// <summary>Loads the report together with its company details and data rows.</summary>
    Task<ActivityReportNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a company's reports in the given range, newest first.</summary>
    Task<List<ActivityReport>> GetCompanyReportsAsync(
        int companyId,
        DateTime? start = null,
        DateTime? end = null,
        CancellationToken cancellationToken = default);
}
