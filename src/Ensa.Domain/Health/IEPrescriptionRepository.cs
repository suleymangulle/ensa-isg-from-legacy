using Ensa.Domain.Repositories;
using Ensa.Domain.Health.Navigations;

namespace Ensa.Domain.Health;

/// <summary>
/// Queries specific to e-prescriptions.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IEPrescriptionRepository : IRepository<EPrescription>
{
    /// <summary>
    /// Loads the prescription together with the patient, the medication lines (with SKRS
    /// medication names) and the diagnosis lines (with ICD-10 names). Returns <c>null</c> when it
    /// does not exist.
    /// </summary>
    Task<EPrescriptionNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>Finds a prescription by its e-prescription code (used to match up service callbacks).</summary>
    Task<EPrescription?> FindByEPrescriptionCodeAsync(
        string ePrescriptionCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the patient's prescription history, newest first.
    /// When <paramref name="companyEmployeeId"/> is supplied it filters through the employee
    /// record, otherwise through <paramref name="patientNationalId"/>.
    /// </summary>
    Task<List<EPrescription>> GetPatientHistoryAsync(
        string? patientNationalId = null,
        int? companyEmployeeId = null,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default);

    /// <summary>Number of prescriptions the physician issued in the given date range (for quotas/reports).</summary>
    Task<int> GetIssuedPrescriptionCountAsync(
        int physicianUserId,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default);
}
