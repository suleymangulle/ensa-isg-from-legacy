using Ensa.Application.Contracts.Common;
using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Documents;
using Ensa.Domain.Repositories;
using Ensa.Domain.Risks;

namespace Ensa.Application.Risks;

/// <summary>
/// Batched reference-data resolution for the Risks module.
/// <para>
/// List and navigation projections need display names for foreign keys. Resolving them one
/// row at a time is the classic N+1; every helper here collects the distinct ids of a whole
/// page first and issues a <b>single</b> <c>IN (...)</c> query, then serves the rows from a
/// dictionary.
/// </para>
/// <para>
/// <c>Ensa.Application</c> does not reference EF Core, so only repository methods and plain
/// LINQ predicates are used — <c>Contains</c> over a local list is translated by the provider.
/// </para>
/// </summary>
internal static class RiskLookupHelper
{
    /// <summary>Wraps an optional id/name pair into a lookup, or <c>null</c> when the id is absent.</summary>
    public static LookupDto? Lookup(int? id, string? displayName)
        => id is null ? null : new LookupDto { Id = id.Value, DisplayName = displayName ?? string.Empty };

    /// <summary>Wraps an optional id into a lookup resolved from a preloaded dictionary.</summary>
    public static LookupDto? Lookup(int? id, IReadOnlyDictionary<int, string> names)
        => id is { } key && names.TryGetValue(key, out var name)
            ? new LookupDto { Id = key, DisplayName = name }
            : Lookup(id, (string?)null);

    /// <summary>Distinct, non-null foreign keys of a page — the input of every batch load below.</summary>
    public static List<int> DistinctIds<T>(IEnumerable<T> source, Func<T, int?> selector)
        => [.. source.Select(selector).Where(id => id is > 0).Select(id => id!.Value).Distinct()];

    /// <summary>Distinct, required foreign keys of a page.</summary>
    public static List<int> DistinctIds<T>(IEnumerable<T> source, Func<T, int> selector)
        => [.. source.Select(selector).Where(id => id > 0).Distinct()];

    public static Task<Dictionary<int, string>> LoadCompanyNamesAsync(
        IReadOnlyRepository<Company> repository,
        List<int> ids,
        CancellationToken cancellationToken)
        => LoadAsync(repository, ids, c => c.CompanyName, cancellationToken);

    public static Task<Dictionary<int, string>> LoadDepartmentNamesAsync(
        IReadOnlyRepository<WorkplaceDepartment> repository,
        List<int> ids,
        CancellationToken cancellationToken)
        => LoadAsync(repository, ids, d => d.DepartmentName, cancellationToken);

    public static Task<Dictionary<int, string>> LoadEmployeeNamesAsync(
        IReadOnlyRepository<CompanyEmployee> repository,
        List<int> ids,
        CancellationToken cancellationToken)
        => LoadAsync(repository, ids, e => $"{e.Name} {e.LastName}".Trim(), cancellationToken);

    public static Task<Dictionary<int, string>> LoadDocumentNamesAsync(
        IReadOnlyRepository<Document> repository,
        List<int> ids,
        CancellationToken cancellationToken)
        => LoadAsync(repository, ids, d => d.DocumentName, cancellationToken);

    public static Task<Dictionary<int, string>> LoadHazardCategoryNamesAsync(
        IReadOnlyRepository<HazardCategory> repository,
        List<int> ids,
        CancellationToken cancellationToken)
        => LoadAsync(repository, ids, c => c.CategoryName, cancellationToken);

    public static Task<Dictionary<int, string>> LoadHazardNamesAsync(
        IReadOnlyRepository<Hazard> repository,
        List<int> ids,
        CancellationToken cancellationToken)
        => LoadAsync(repository, ids, h => h.HazardTag, cancellationToken);

    public static Task<Dictionary<int, string>> LoadEquipmentDocumentTypeNamesAsync(
        IReadOnlyRepository<EquipmentDocumentType> repository,
        List<int> ids,
        CancellationToken cancellationToken)
        => LoadAsync(repository, ids, t => t.DocumentName, cancellationToken);

    /// <summary>Single <c>IN (...)</c> round trip; returns an empty map for an empty id set.</summary>
    private static async Task<Dictionary<int, string>> LoadAsync<TEntity>(
        IReadOnlyRepository<TEntity> repository,
        List<int> ids,
        Func<TEntity, string> displayNameSelector,
        CancellationToken cancellationToken)
        where TEntity : class, IEntity<int>
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var records = await repository.GetListAsync(e => ids.Contains(e.Id), cancellationToken);

        var result = new Dictionary<int, string>(records.Count);
        foreach (var record in records)
        {
            result[record.Id] = displayNameSelector(record);
        }

        return result;
    }
}
