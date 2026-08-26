using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Documents;

/// <summary>
/// Queries specific to the <see cref="Archive"/> module.
/// </summary>
public class ArchiveRepository(EnsaDbContext context, IDataFilter dataFilter)
    : EfCoreRepository<Archive>(context, dataFilter), IArchiveRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// <paramref name="month"/> and <paramref name="year"/> are added to the predicate only when a value
    /// is supplied; this way EF Core emits correct (and index-friendly) SQL for every combination instead
    /// of a parameter-insensitive plan such as <c>@month IS NULL OR ...</c>.
    /// </remarks>
    public Task<List<Archive>> GetByModuleAsync(
        DocumentOwnerType moduleType,
        int moduleId,
        int? month = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetReadOnlyQueryable()
            .Where(a => a.ModuleType == moduleType && a.ModuleId == moduleId);

        if (month is int monthValue)
        {
            query = query.Where(a => a.Month == monthValue);
        }

        if (year is int yearValue)
        {
            query = query.Where(a => a.Year == yearValue);
        }

        return query
            .OrderByDescending(a => a.Year)
            .ThenByDescending(a => a.Month)
            .ThenByDescending(a => a.Id)
            .ToListAsync(cancellationToken);
    }
}
