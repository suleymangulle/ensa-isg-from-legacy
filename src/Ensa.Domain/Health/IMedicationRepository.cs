using Ensa.Domain.Repositories;

namespace Ensa.Domain.Health;

/// <summary>
/// Queries specific to the SKRS medication reference table (a host table — read only).
/// <para>
/// Legacy equivalent: <c>Businness.EPrescriptionOperations.SKRS_Operations.GetMedicationList</c>.
/// </para>
/// </summary>
public interface IMedicationRepository : IReadOnlyRepository<Medication>
{
    /// <summary>
    /// Searches by exact barcode match or by a term contained in the medication name.
    /// Only active medications are returned.
    /// <para>
    /// The legacy behaviour is preserved: the name search runs both against the text as entered
    /// and against a form with the Turkish characters folded, and the results are ordered by
    /// where the match occurs.
    /// </para>
    /// </summary>
    /// <param name="filter">A barcode, or part of a medication name.</param>
    /// <param name="maxResultCount">Maximum number of records to return (the legacy default was 25).</param>
    Task<List<Medication>> SearchByBarcodeOrNameAsync(
        string filter,
        int maxResultCount = 25,
        CancellationToken cancellationToken = default);

    /// <summary>Bulk-loads the entries for the given medication ids (for rendering a prescription).</summary>
    Task<List<Medication>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default);
}
