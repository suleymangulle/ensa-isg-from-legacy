using Ensa.Domain.Health;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Health;

/// <summary>
/// EF Core implementation of <see cref="IMedicationRepository"/>.
/// <para>
/// The SKRS medication list is a host (tenant-less) reference table; it is read only.
/// </para>
/// </summary>
public class MedicationRepository(EnsaDbContext context)
    : EfCoreReadOnlyRepository<Medication>(context), IMedicationRepository
{
    /// <summary>Upper bound that keeps lookup screens from flooding the query.</summary>
    private const int AbsoluteMaxResultCount = 500;

    /// <inheritdoc />
    /// <remarks>
    /// The legacy behaviour is preserved: exact barcode matches rank first, the name search runs both
    /// against the text as entered and against its Turkish-character-folded form, and the results are
    /// ordered by match position (<c>CHARINDEX</c>). Ordering and capping happen <b>in the database</b>.
    /// </remarks>
    public Task<List<Medication>> SearchByBarcodeOrNameAsync(
        string filter,
        int maxResultCount = 25,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return Task.FromResult(new List<Medication>());
        }

        var takeCount = Math.Clamp(maxResultCount, 1, AbsoluteMaxResultCount);

        var search = filter.Trim();
        var likeSearch = TurkishSearch.EscapeLike(search);
        var normalized = TurkishSearch.Simplify(likeSearch);

        var pattern = $"%{likeSearch}%";
        var normalizedPattern = $"%{normalized}%";

        var query = GetReadOnlyQueryable().Where(x => x.IsActive);

        query = string.Equals(likeSearch, normalized, StringComparison.Ordinal)
            ? query.Where(x => x.Barcode == search || EF.Functions.Like(x.MedicationName, pattern))
            : query.Where(x => x.Barcode == search
                               || EF.Functions.Like(x.MedicationName, pattern)
                               || EF.Functions.Like(x.MedicationName, normalizedPattern));

        return query
            // Exact barcode matches rank first, then the closest match at the start of the name.
            .OrderByDescending(x => x.Barcode == search)
            .ThenBy(x => x.MedicationName.IndexOf(search))
            .ThenBy(x => x.MedicationName)
            .Take(takeCount)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>A single <c>IN</c> query — no query is issued per id.</remarks>
    public Task<List<Medication>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var list = ids.Distinct().ToList();
        if (list.Count == 0)
        {
            return Task.FromResult(new List<Medication>());
        }

        return GetReadOnlyQueryable()
            .Where(x => list.Contains(x.Id))
            .OrderBy(x => x.MedicationName)
            .ToListAsync(cancellationToken);
    }
}
