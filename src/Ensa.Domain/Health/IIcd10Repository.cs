using Ensa.Domain.Repositories;

namespace Ensa.Domain.Health;

/// <summary>
/// Queries specific to the ICD-10 reference table (a host table — read only; the data is seeded
/// from SKRS by <c>DbMigrator</c>).
/// <para>
/// Legacy equivalent: <c>Businness.EPrescriptionOperations.SKRS_Operations.GetICD10List</c>.
/// </para>
/// </summary>
public interface IIcd10Repository : IReadOnlyRepository<Icd10>
{
    /// <summary>
    /// Searches for the given term in the diagnosis name or code.
    /// <para>
    /// The legacy behaviour is preserved: the search runs both against the text as entered and
    /// against a form with the Turkish characters folded, and the results come back ordered by
    /// code then name.
    /// </para>
    /// </summary>
    /// <param name="filter">The search term (part of a diagnosis name, or an ICD-10 code).</param>
    /// <param name="maxResultCount">Maximum number of records to return (the legacy default was 25).</param>
    Task<List<Icd10>> SearchAsync(
        string filter,
        int maxResultCount = 25,
        CancellationToken cancellationToken = default);

    /// <summary>Bulk-loads the entries for the given ICD-10 codes (for rendering a prescription).</summary>
    Task<List<Icd10>> GetByCodesAsync(
        IEnumerable<string> codes,
        CancellationToken cancellationToken = default);
}
