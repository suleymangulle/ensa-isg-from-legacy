using System.Security.Cryptography;
using System.Globalization;
using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Documents;
using Ensa.Application.Contracts.Documents.Dtos;
using Ensa.Application.Contracts.Documents.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Documents;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Documents;

/// <summary>
/// Metadata management for the central document store.
/// <para>
/// <b>Transfer.</b> <see cref="UploadAsync"/> and <see cref="DownloadAsync"/> move the bytes;
/// <c>IDocumentStorage</c> decides where they live. Payloads at or below 256 KB stay in the row,
/// larger ones go to the storage provider. Size and SHA-256 are measured from what actually
/// arrives, the storage key is generated, and downloads are always attachments with executable
/// content types neutralised. See ADR-026.
/// </para>
/// </summary>
public class DocumentAppService(
    IServiceProvider serviceProvider,
    IDocumentRepository documentRepository,
    IDocumentStorage documentStorage)
    : EnsaAppService(serviceProvider), IDocumentAppService
{
    /// <inheritdoc />
    public async Task<DocumentNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Document.Default);

        var navigation = await documentRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(Document), id);

        return new DocumentNavigationDto
        {
            Document = ObjectMapper.Map<Document, DocumentDto>(navigation.Document),
            Category = navigation.Category is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Category.Id,
                    DisplayName = navigation.Category.CategoryName,
                    Code = navigation.Category.CategoryCode
                },
            // CompanyId without a name means the company row is gone; the id is still worth
            // returning so the screen can show that the link is dangling.
            Company = navigation.Document.CompanyId is { } companyId
                ? new LookupDto { Id = companyId, DisplayName = navigation.CompanyName ?? string.Empty }
                : null
        };
    }

    /// <inheritdoc />
    public async Task<DocumentDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Document.Default);

        var document = await documentRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Document), id);

        return ObjectMapper.Map<Document, DocumentDto>(document);
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<DocumentListDto>> GetListAsync(
        GetDocumentListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Document.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "CreationTime DESC");

        var total = await documentRepository.GetCountAsync(predicate, cancellationToken);

        var records = await documentRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Document>, List<DocumentListDto>>(records);

        return new PagedResultDto<DocumentListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<DocumentListDto>> GetByOwnerAsync(
        DocumentOwnerType ownerType,
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Document.Default);

        var records = await documentRepository.GetByOwnerAsync(ownerType, ownerId, cancellationToken);

        var items = ObjectMapper.Map<List<Document>, List<DocumentListDto>>(records);

        return new ListResultDto<DocumentListDto>(items);
    }

    /// <inheritdoc />
    public async Task<DocumentDto?> FindBySha256Async(
        string sha256,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);
        await CheckPermissionAsync(EnsaPermissions.Document.Default);

        var document = await documentRepository.FindBySha256Async(sha256.Trim(), cancellationToken);

        // A miss is the normal answer for a duplicate check, so this returns null instead of
        // throwing EntityNotFoundException.
        return document is null ? null : ObjectMapper.Map<Document, DocumentDto>(document);
    }

    /// <inheritdoc />
    public async Task<DocumentDto> CreateAsync(
        CreateDocumentDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Document.Create);

        var document = ObjectMapper.Map<CreateDocumentDto, Document>(input);

        // The storage name is server-generated and never taken from the request: it is the
        // key under which the binary will be written, so letting a caller choose it would
        // reopen the path-traversal and collision problems the column exists to prevent.
        document.StorageName = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

        await documentRepository.InsertAsync(document, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Document metadata created: {DocumentId} - {DocumentName}",
            document.Id, document.DocumentName);

        return ObjectMapper.Map<Document, DocumentDto>(document);
    }

    /// <inheritdoc />
    public async Task<DocumentDto> UpdateAsync(
        int id,
        UpdateDocumentDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Document.Update);

        var document = await documentRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Document), id);

        // The mapper leaves StorageName, StoragePath and Content untouched, so an update can
        // never repoint a metadata row at a different stored file.
        ObjectMapper.Map(input, document);

        await documentRepository.UpdateAsync(document, autoSave: true, cancellationToken);

        return ObjectMapper.Map<Document, DocumentDto>(document);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Document.Delete);

        var document = await documentRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Document), id);

        // Soft delete only, and the payload is intentionally left where it is. A soft-deleted
        // row can be restored, and a restore that finds its file gone would be worse than the
        // disk it saves. Reclaiming the bytes is a sweep over rows deleted long enough ago to be
        // beyond recovery - a background job, not part of this request.
        await documentRepository.DeleteAsync(document, autoSave: true, cancellationToken);

        Logger.LogInformation("Document metadata deleted: {DocumentId}", id);
    }

    /// <summary>
    /// Payloads at or below this size are kept in the database row rather than on the file
    /// system. A logo or a signature image is not worth a second storage round trip, and keeping
    /// it in the row means a database backup is self-contained.
    /// </summary>
    private const int InlineContentMaxBytes = 256 * 1024;

    /// <summary>
    /// Content types a browser will happily execute in the origin's context. They are never
    /// served back verbatim; see <see cref="DownloadAsync"/>.
    /// </summary>
    private static readonly string[] ExecutableContentTypes =
    [
        "text/html", "application/xhtml+xml", "image/svg+xml",
        "application/xml", "text/xml", "application/javascript", "text/javascript",
    ];

    /// <inheritdoc />
    public async Task<DocumentDto> UploadAsync(
        UploadDocumentDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Document.Create);

        var fileName = SafeFileName(input.FileName);

        // Read once into a seekable buffer so the digest can be computed before deciding where
        // the bytes go. The upload ceiling is enforced by the storage provider while streaming;
        // this buffer only ever holds a payload the request already accepted.
        await using var buffered = new MemoryStream();
        await input.Content.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;

        if (buffered.Length == 0)
        {
            throw new BusinessException("The uploaded file is empty.", "Ensa:Document:EmptyFile");
        }

        var digest = Convert.ToHexStringLower(await SHA256.HashDataAsync(buffered, cancellationToken));
        buffered.Position = 0;

        // The same bytes already stored means a duplicate, not a second document. Rejecting is
        // kinder than silently creating a twin nobody can tell apart afterwards.
        var existing = await documentRepository.FindBySha256Async(digest, cancellationToken);
        if (existing is not null)
        {
            throw new BusinessException(
                    "This file has already been uploaded.",
                    "Ensa:Document:DuplicateContent")
                .WithData("DocumentName", existing.DocumentName)
                .WithData("DocumentId", existing.Id);
        }

        var storageName = Guid.NewGuid().ToString("N");
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

        var document = new Document
        {
            DocumentName = fileName,
            Extension = extension.Length > 0 ? extension : null,
            ContentType = input.ContentType,
            SizeBytes = buffered.Length,
            Sha256 = digest,
            StorageName = storageName,
            DocumentCategoryId = input.DocumentCategoryId,
            CompanyId = input.CompanyId,
            OwnerType = input.OwnerType,
            OwnerRecordId = input.OwnerRecordId,
            IsActive = true,
        };

        if (buffered.Length <= InlineContentMaxBytes)
        {
            document.Content = buffered.ToArray();
        }
        else
        {
            document.StoragePath = await documentStorage.SaveAsync(
                storageName, CurrentTenant.Id, buffered, cancellationToken);
        }

        try
        {
            document = await documentRepository.InsertAsync(document, autoSave: true, cancellationToken);
        }
        catch
        {
            // Without this the payload would outlive the failed insert as an orphan that nothing
            // references and nothing will ever clean up.
            if (document.StoragePath is { } orphan)
            {
                await documentStorage.DeleteAsync(orphan, cancellationToken);
            }

            throw;
        }

        Logger.LogInformation(
            "Document uploaded: {DocumentId}, {SizeBytes} bytes, inline={Inline}",
            document.Id, document.SizeBytes, document.Content is not null);

        return ObjectMapper.Map<Document, DocumentDto>(document);
    }

    /// <inheritdoc />
    public async Task<DocumentContentDto> DownloadAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Document.Default);

        // The repository's tenant filter is what stops one organization reading another's files;
        // the storage layer knows nothing about tenants.
        var document = await documentRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Document), id);

        Stream content;

        if (document.Content is { } inline)
        {
            content = new MemoryStream(inline, writable: false);
        }
        else if (document.StoragePath is { } path)
        {
            content = await documentStorage.OpenAsync(path, cancellationToken)
                      ?? throw new BusinessException(
                              "The stored file is missing.",
                              "Ensa:Document:ContentMissing")
                          .WithData("DocumentId", id);
        }
        else
        {
            throw new BusinessException(
                    "This document has no content yet.",
                    "Ensa:Document:NoContent")
                .WithData("DocumentId", id);
        }

        Logger.LogInformation("Document downloaded: {DocumentId}", id);

        return new DocumentContentDto
        {
            FileName = SafeFileName(document.DocumentName),
            ContentType = SafeContentType(document.ContentType),
            SizeBytes = document.SizeBytes,
            Content = content,
        };
    }

    /// <summary>
    /// Strips any directory part from a file name.
    /// <para>
    /// The name never builds a storage path — the GUID key does — but it does end up in a
    /// <c>Content-Disposition</c> header, so it must not carry separators, quotes or control
    /// characters.
    /// </para>
    /// </summary>
    private static string SafeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName?.Trim() ?? string.Empty);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new EnsaValidationException(
                nameof(UploadDocumentDto.FileName), "A file name is required.");
        }

        var cleaned = new string([.. name.Where(character => !char.IsControl(character) && character != '"')]);

        return string.IsNullOrWhiteSpace(cleaned) ? "download" : cleaned;
    }

    /// <summary>
    /// Neutralises content types a browser would execute.
    /// <para>
    /// An uploaded <c>.html</c> or <c>.svg</c> served back with its declared type runs in this
    /// application's own origin — stored cross-site scripting, using the uploader's file. Every
    /// download is an attachment, and these types additionally become
    /// <c>application/octet-stream</c> so nothing renders them inline.
    /// </para>
    /// </summary>
    private static string SafeContentType(string? declared)
    {
        if (string.IsNullOrWhiteSpace(declared))
        {
            return "application/octet-stream";
        }

        var value = declared.Trim().ToLowerInvariant();

        return ExecutableContentTypes.Contains(value) ? "application/octet-stream" : value;
    }

    // ----------------------------------------------------------- internals

    private static Expression<Func<Document, bool>> BuildFilter(GetDocumentListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var categoryId = input.DocumentCategoryId;
        var companyId = input.CompanyId;
        var ownerType = input.OwnerType;
        var ownerRecordId = input.OwnerRecordId;
        var isActive = input.IsActive;

        return d =>
            (search == null || d.DocumentName.Contains(search))
            && (categoryId == null || d.DocumentCategoryId == categoryId)
            && (companyId == null || d.CompanyId == companyId)
            && (ownerType == null || d.OwnerType == ownerType)
            && (ownerRecordId == null || d.OwnerRecordId == ownerRecordId)
            && (isActive == null || d.IsActive == isActive);
    }
}
