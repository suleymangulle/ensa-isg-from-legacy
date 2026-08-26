using Ensa.Domain.Repositories;
using Ensa.Domain.Risks.Navigations;

namespace Ensa.Domain.Risks;

/// <summary>Queries specific to field observation reports.</summary>
public interface IFieldObservationReportRepository : IRepository<FieldObservationReport>
{
    /// <summary>Loads the report with its department, lines, line documents and the corrective actions derived from it.</summary>
    Task<FieldObservationReportNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>Lists a company's field observation reports in the given date range.</summary>
    Task<List<FieldObservationReport>> GetListByCompanyAsync(
        int companyId,
        DateTime? start = null,
        DateTime? end = null,
        int? departmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the non-conformity lines of a report.</summary>
    Task<List<FieldObservationLine>> GetLinesAsync(
        int fieldObservationReportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists non-conformity lines whose deadline has passed and whose derived corrective action is
    /// still open.
    /// </summary>
    Task<List<FieldObservationLine>> GetDeadlineElapsedLinesAsync(
        DateTime reference,
        int? companyId = null,
        CancellationToken cancellationToken = default);
}
