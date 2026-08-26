using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Reports;

/// <summary>
/// Module-specific queries for <see cref="OhsReport"/>.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IOhsReportRepository : IRepository<OhsReport>
{
    /// <summary>
    /// Returns the report's hazard class breakdown, read from the
    /// <see cref="OhsReportHazardClassBreakdown"/> child table.
    /// </summary>
    Task<Dictionary<HazardClass, int>> GetHazardClassBreakdownAsync(
        int ohsReportId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns an office's OHS reports for the given period.</summary>
    Task<List<OhsReport>> GetOfficeReportsAsync(
        int officeId,
        DateTime? start = null,
        DateTime? end = null,
        CancellationToken cancellationToken = default);
}
