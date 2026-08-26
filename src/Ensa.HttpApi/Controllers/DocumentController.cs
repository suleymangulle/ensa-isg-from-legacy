using Ensa.Domain.Shared.Exceptions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Documents;
using Ensa.Application.Contracts.Documents.Dtos;
using Ensa.Application.Contracts.Documents.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Document metadata endpoints - <c>api/document</c>.
/// <para>
/// <b>No upload or download endpoint is exposed yet.</b> Transferring the binary needs a
/// storage abstraction (database versus blob store, streaming, size and MIME allow-lists,
/// malware scanning, expiring URLs) that the solution does not have; adding it ad hoc here
/// would freeze one storage strategy into the public API. These endpoints therefore manage the
/// metadata row only.
/// </para>
/// </summary>
public class DocumentController(IDocumentAppService documentAppService) : EnsaController
{
    /// <summary>Hard ceiling for one upload request, mirroring <c>DocumentStorageOptions.MaxSizeBytes</c>.</summary>
    private const long MaxUploadBytes = 25 * 1024 * 1024;

    /// <summary>
    /// Uploads a file and creates its metadata row.
    /// <para>
    /// Multipart request: <c>file</c> carries the payload, the remaining form fields carry the
    /// metadata. Size and digest are measured from what actually arrives, so neither can be
    /// claimed by the client.
    /// </para>
    /// </summary>
    [HttpPost("upload")]
    [Authorize(EnsaPermissions.Document.Create)]
    [ProducesResponseType<DocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<DocumentDto> UploadAsync(
        IFormFile file,
        [FromForm] int? documentCategoryId,
        [FromForm] int? companyId,
        [FromForm] DocumentOwnerType ownerType,
        [FromForm] int? ownerRecordId,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new BusinessException("The uploaded file is empty.", "Ensa:Document:EmptyFile");
        }

        await using var content = file.OpenReadStream();

        return await documentAppService.UploadAsync(
            new UploadDocumentDto
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                Content = content,
                DocumentCategoryId = documentCategoryId,
                CompanyId = companyId,
                OwnerType = ownerType,
                OwnerRecordId = ownerRecordId,
            },
            cancellationToken);
    }

    /// <summary>
    /// Downloads the stored file.
    /// <para>
    /// Always served as an attachment, and a content type a browser would execute is replaced
    /// with <c>application/octet-stream</c> — an uploaded HTML or SVG file rendered inline would
    /// run in this application's origin.
    /// </para>
    /// </summary>
    [HttpGet("{id:int}/content")]
    [Authorize(EnsaPermissions.Document.Default)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAsync(int id, CancellationToken cancellationToken)
    {
        var content = await documentAppService.DownloadAsync(id, cancellationToken);

        // FileStreamResult disposes the stream once the response has been written.
        return File(content.Content, content.ContentType, content.FileName);
    }

    /// <summary>Returns the metadata of a single document.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.Document.Default)]
    [ProducesResponseType<DocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<DocumentDto> GetAsync(int id, CancellationToken cancellationToken)
        => documentAppService.GetAsync(id, cancellationToken);

    /// <summary>Returns the document together with its category and owning company.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.Document.Default)]
    [ProducesResponseType<DocumentNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<DocumentNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => documentAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable document list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Document.Default)]
    [ProducesResponseType<PagedResultDto<DocumentListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<DocumentListDto>> GetListAsync(
        [FromQuery] GetDocumentListInput input,
        CancellationToken cancellationToken)
        => documentAppService.GetListAsync(input, cancellationToken);

    /// <summary>Every file attached to one polymorphic owner record.</summary>
    [HttpGet("by-owner/{ownerType}/{ownerId:int}")]
    [Authorize(EnsaPermissions.Document.Default)]
    [ProducesResponseType<ListResultDto<DocumentListDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<DocumentListDto>> GetByOwnerAsync(
        DocumentOwnerType ownerType,
        int ownerId,
        CancellationToken cancellationToken)
        => documentAppService.GetByOwnerAsync(ownerType, ownerId, cancellationToken);

    /// <summary>
    /// Looks a document up by the SHA-256 digest of its content, for duplicate detection.
    /// Responds with <c>204 No Content</c> when nothing matches.
    /// </summary>
    [HttpGet("by-hash/{sha256}")]
    [Authorize(EnsaPermissions.Document.Default)]
    [ProducesResponseType<DocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<DocumentDto?> FindBySha256Async(string sha256, CancellationToken cancellationToken)
        => documentAppService.FindBySha256Async(sha256, cancellationToken);

    /// <summary>Creates the metadata row. The binary is transferred separately.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.Document.Create)]
    [ProducesResponseType<DocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<DocumentDto> CreateAsync(
        [FromBody] CreateDocumentDto input,
        CancellationToken cancellationToken)
        => documentAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates the metadata. The storage coordinates cannot be changed.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.Document.Update)]
    [ProducesResponseType<DocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<DocumentDto> UpdateAsync(
        int id,
        [FromBody] UpdateDocumentDto input,
        CancellationToken cancellationToken)
        => documentAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the metadata row (soft delete).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.Document.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => documentAppService.DeleteAsync(id, cancellationToken);
}
