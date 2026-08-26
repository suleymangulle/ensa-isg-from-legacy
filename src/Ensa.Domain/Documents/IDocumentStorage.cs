namespace Ensa.Domain.Documents;

/// <summary>
/// Where the bytes of a <see cref="Document"/> actually live.
/// <para>
/// The metadata row and the payload are deliberately separate concerns: the row is
/// transactional and belongs in the database, while the payload is large, immutable once
/// written, and cheaper to keep outside it. This abstraction is what lets the file system be
/// swapped for blob storage without any caller changing.
/// </para>
/// <para>
/// <b>Implementations must never build a path from caller-supplied text.</b> The only key is
/// <see cref="Document.StorageName"/>, a GUID the system generates, precisely so that a file
/// called <c>../../appsettings.json</c> cannot become a path. An implementation that resolves a
/// key outside its own root must refuse rather than trust it.
/// </para>
/// </summary>
public interface IDocumentStorage
{
    /// <summary>
    /// Writes the content and returns the relative path to record in
    /// <see cref="Document.StoragePath"/>.
    /// </summary>
    /// <param name="storageName">The system-generated GUID key. Never a user-supplied name.</param>
    /// <param name="tenantId">Owning tenant; <c>null</c> for host-level documents.</param>
    /// <param name="content">The payload. Read from the current position to the end.</param>
    Task<string> SaveAsync(
        string storageName,
        int? tenantId,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the content for reading, or returns <c>null</c> when the payload is missing — a
    /// metadata row can outlive its file after a restore or a manual clean-up, and the caller
    /// needs to tell that apart from "no such document".
    /// </summary>
    Task<Stream?> OpenAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the payload. Missing content is not an error: deletion has to be safe to retry.
    /// </summary>
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}
