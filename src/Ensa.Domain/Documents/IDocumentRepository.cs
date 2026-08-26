using Ensa.Domain.Documents.Navigations;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Documents;

/// <summary>
/// Module-specific repository contract for <see cref="Document"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface IDocumentRepository : IRepository<Document>
{
    /// <summary>
    /// Loads the document as a combined view with its category and the name of the company it
    /// belongs to. <c>null</c> when the document does not exist.
    /// </summary>
    Task<DocumentNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every document registered against the polymorphic owner
    /// (<paramref name="ownerType"/> + <paramref name="ownerRecordId"/>).
    /// </summary>
    Task<List<Document>> GetByOwnerAsync(
        DocumentOwnerType ownerType,
        int ownerRecordId,
        CancellationToken cancellationToken = default);

    /// <summary>Looks a document up by its SHA-256 digest, to detect duplicates.</summary>
    Task<Document?> FindBySha256Async(string sha256, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the total document size, for storage quota and reporting purposes.
    /// When <paramref name="companyId"/> is supplied, only that company's documents are counted.
    /// </summary>
    Task<long> GetTotalSizeAsync(int? companyId = null, CancellationToken cancellationToken = default);
}
