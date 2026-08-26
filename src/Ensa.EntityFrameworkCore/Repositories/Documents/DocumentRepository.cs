using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Documents;
using Ensa.Domain.Documents.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Documents;

/// <summary>
/// Queries specific to the <see cref="Document"/> module.
/// <para>
/// Because <see cref="Document"/> is both tenant and soft-delete filtered, the queries below never write a
/// <c>TenantId</c> / <c>IsDeleted</c> predicate; the global query filters take care of that.
/// </para>
/// </summary>
public class DocumentRepository(EnsaDbContext context, IDataFilter dataFilter)
    : EfCoreRepository<Document>(context, dataFilter), IDocumentRepository
{

    /// <inheritdoc />
    /// <remarks>
    /// Three queries at most — document, category, company — and never one per row, because a
    /// document has exactly one of each.
    /// </remarks>
    public async Task<DocumentNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var document = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (document is null)
        {
            return null;
        }

        var navigation = new DocumentNavigation { Document = document };

        if (document.DocumentCategoryId is { } categoryId)
        {
            navigation.Category = await Context.Set<DocumentCategory>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
        }

        if (document.CompanyId is { } companyId)
        {
            // Only the display name crosses the module boundary; the Company entity itself does not.
            navigation.CompanyName = await Context.Set<Company>()
                .AsNoTracking()
                .Where(c => c.Id == companyId)
                .Select(c => c.CompanyName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return navigation;
    }
    /// <inheritdoc />
    public Task<List<Document>> GetByOwnerAsync(
        DocumentOwnerType ownerType,
        int ownerRecordId,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .Where(d => d.OwnerType == ownerType && d.OwnerRecordId == ownerRecordId && d.IsActive)
            .OrderByDescending(d => d.CreationTime)
            .ThenByDescending(d => d.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// More than one record may share the same hash (identical content owned by different records);
    /// the oldest record is taken as the reference for duplicate detection.
    /// </remarks>
    public Task<Document?> FindBySha256Async(string sha256, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sha256))
        {
            return Task.FromResult<Document?>(null);
        }

        return GetReadOnlyQueryable()
            .Where(d => d.Sha256 == sha256)
            .OrderBy(d => d.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The <c>SUM</c> is computed in the database. Since SQL returns <c>NULL</c> when there are no rows,
    /// the projection is taken as <c>long?</c> and coalesced to <c>0</c>.
    /// </remarks>
    public async Task<long> GetTotalSizeAsync(
        int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetReadOnlyQueryable().Where(d => d.IsActive);

        if (companyId is int value)
        {
            query = query.Where(d => d.CompanyId == value);
        }

        return await query.SumAsync(d => (long?)d.SizeBytes, cancellationToken) ?? 0L;
    }
}
