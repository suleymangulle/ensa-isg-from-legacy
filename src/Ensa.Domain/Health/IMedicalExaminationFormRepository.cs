using Ensa.Domain.Repositories;
using Ensa.Domain.Health.Navigations;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Health;

/// <summary>
/// Queries specific to the medical examination form.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IMedicalExaminationFormRepository : IRepository<MedicalExaminationForm>
{
    /// <summary>
    /// Loads the form together with the employee, the workplace and every normalised child list.
    /// Returns <c>null</c> when it does not exist.
    /// </summary>
    Task<MedicalExaminationFormNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the employee's most recent examination form (ordered by <c>ExaminationDate</c>
    /// descending).
    /// </summary>
    /// <param name="companyEmployeeId">The employee record.</param>
    /// <param name="reportType">When supplied, only examinations of this type are considered.</param>
    Task<MedicalExaminationForm?> GetLatestExaminationAsync(
        int companyEmployeeId,
        MedicalReportType? reportType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the examination forms at a workplace whose validity has expired (or is about to
    /// expire) as of <paramref name="referenceDate"/> — for periodic follow-up reminders.
    /// </summary>
    /// <param name="companyId">The workplace.</param>
    /// <param name="referenceDate">Comparison date; <c>ValidityDate &lt;= referenceDate</c>.</param>
    /// <param name="maxResultCount">Maximum number of records to return.</param>
    Task<List<MedicalExaminationForm>> GetDurationExpiredAsync(
        int companyId,
        DateTime referenceDate,
        int maxResultCount = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the forms in the given IBYS status — for the IBYS bulk submission job.
    /// </summary>
    Task<List<MedicalExaminationForm>> GetByIbysStatusAsync(
        IbysSubmissionStatus status,
        int maxResultCount = 100,
        CancellationToken cancellationToken = default);
}
