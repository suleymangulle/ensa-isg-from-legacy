using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Documents.Dtos;

/// <summary>
/// An upload: the payload plus where it belongs.
/// <para>
/// The stream is read once and never buffered whole, so a large file does not have to fit in
/// memory. <b>The caller owns the stream</b> and disposes it; the service only reads from it.
/// </para>
/// <para>
/// Size, digest and storage key are NOT inputs. The size is measured while reading, the SHA-256
/// is computed from what actually arrived, and the storage key is generated — a client cannot
/// claim a size it did not send, nor choose where its bytes land.
/// </para>
/// </summary>
public class UploadDocumentDto
{
    /// <summary>Original file name as the uploader sees it. Used to name the download, never to build a path.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>MIME type declared by the client. Advisory only; downloads are always sent as attachments.</summary>
    public string? ContentType { get; set; }

    /// <summary>The payload.</summary>
    public Stream Content { get; set; } = Stream.Null;

    public int? DocumentCategoryId { get; set; }

    public int? CompanyId { get; set; }

    public DocumentOwnerType OwnerType { get; set; } = DocumentOwnerType.Unspecified;

    public int? OwnerRecordId { get; set; }
}

/// <summary>
/// A download: the metadata a response needs plus the content itself.
/// <para>
/// <b>The caller must dispose <see cref="Content"/>.</b> It is an open handle on the payload,
/// not a buffer, so that a 20 MB file streams to the client instead of being loaded first.
/// </para>
/// </summary>
public sealed class DocumentContentDto : IDisposable
{
    /// <summary>Name to offer the browser. Sanitised — it is used in a response header.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Content type to serve with. Never the declared type verbatim; see the service.</summary>
    public string ContentType { get; set; } = "application/octet-stream";

    public long SizeBytes { get; set; }

    public Stream Content { get; set; } = Stream.Null;

    public void Dispose() => Content.Dispose();
}
