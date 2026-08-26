using Ensa.Domain.Health;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Health;

/// <summary>
/// EF Core implementation of <see cref="IIcd10Repository"/>.
/// <para>
/// ICD-10 is a host (tenant-less) reference table; it is read only and seeded by <c>DbMigrator</c>. That is
/// why the base class is <see cref="EfCoreReadOnlyRepository{TEntity}"/>.
/// </para>
/// </summary>
public class Icd10Repository(EnsaDbContext context)
    : EfCoreReadOnlyRepository<Icd10>(context), IIcd10Repository
{
    /// <summary>Upper bound that keeps lookup screens from flooding the query.</summary>
    private const int AbsoluteMaxResultCount = 500;

    /// <inheritdoc />
    /// <remarks>
    /// The legacy behaviour is preserved: the search runs both against the text as entered and against
    /// its Turkish-character-folded form (see <see cref="TurkishSearch"/>). The result set is capped in
    /// the database with <c>TOP</c>; the table is never loaded into memory.
    /// </remarks>
    public Task<List<Icd10>> SearchAsync(
        string filter,
        int maxResultCount = 25,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return Task.FromResult(new List<Icd10>());
        }

        var takeCount = Math.Clamp(maxResultCount, 1, AbsoluteMaxResultCount);

        var search = TurkishSearch.EscapeLike(filter.Trim());
        var normalized = TurkishSearch.Simplify(search);

        var pattern = $"%{search}%";
        var normalizedPattern = $"%{normalized}%";

        var query = GetReadOnlyQueryable().Where(x => x.IsActive);

        query = string.Equals(search, normalized, StringComparison.Ordinal)
            ? query.Where(x => EF.Functions.Like(x.Name, pattern) || EF.Functions.Like(x.Code, pattern))
            : query.Where(x => EF.Functions.Like(x.Name, pattern)
                               || EF.Functions.Like(x.Code, pattern)
                               || EF.Functions.Like(x.Name, normalizedPattern)
                               || EF.Functions.Like(x.Code, normalizedPattern));

        return query
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .Take(takeCount)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>A single <c>IN</c> query — no query is issued per code.</remarks>
    public Task<List<Icd10>> GetByCodesAsync(
        IEnumerable<string> codes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(codes);

        var list = codes
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (list.Count == 0)
        {
            return Task.FromResult(new List<Icd10>());
        }

        return GetReadOnlyQueryable()
            .Where(x => list.Contains(x.Code))
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }
}
