using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Documents.Dtos;
using Ensa.Application.Contracts.Documents.Dtos.Navigations;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Documents;

/// <summary>
/// Metadata management for the central document store.
/// <para>
/// <b>Transfer.</b> <see cref="UploadAsync"/> and <see cref="DownloadAsync"/> move the bytes.
/// Where they live is the storage provider's decision: small payloads stay in the row, larger
/// ones go to a file or blob store. The client never chooses the storage key, never states the
/// size, and never receives an executable content type back. See ADR-026.
/// </para>
/// </summary>
public interface IDocumentAppService : IApplicationService
{
    Task<DocumentDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The document with its category and owning company resolved, for the detail screen.
    /// </summary>
    Task<DocumentNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<DocumentListDto>> GetListAsync(
        GetDocumentListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every file attached to one polymorphic owner, e.g. all documents of a given employee.
    /// </summary>
    Task<ListResultDto<DocumentListDto>> GetByOwnerAsync(
        DocumentOwnerType ownerType,
        int ownerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an existing document by the SHA-256 digest of its content, so a caller can detect
    /// a duplicate before uploading the same bytes again. Returns <c>null</c> when no match
    /// exists - a miss is an ordinary outcome here, not an error.
    /// </summary>
    Task<DocumentDto?> FindBySha256Async(string sha256, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores an uploaded file and creates its metadata row in one step.
    /// <para>
    /// Size and SHA-256 are measured from what actually arrives, never taken from the request,
    /// and the storage key is generated. When the digest matches an existing document the upload
    /// is rejected rather than silently duplicated — the caller can look the original up with
    /// <see cref="FindBySha256Async"/> and link to it instead.
    /// </para>
    /// </summary>
    Task<DocumentDto> UploadAsync(UploadDocumentDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the stored payload for download. The returned content is an open stream the caller
    /// must dispose.
    /// </summary>
    Task<DocumentContentDto> DownloadAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the metadata row. The binary is transferred separately.</summary>
    Task<DocumentDto> CreateAsync(CreateDocumentDto input, CancellationToken cancellationToken = default);

    Task<DocumentDto> UpdateAsync(int id, UpdateDocumentDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
