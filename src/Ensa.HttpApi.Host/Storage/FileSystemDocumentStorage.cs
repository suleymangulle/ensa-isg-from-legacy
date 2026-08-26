using Ensa.Domain.Documents;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Options;

namespace Ensa.HttpApi.Host.Storage;

/// <summary>Configuration for <see cref="FileSystemDocumentStorage"/>.</summary>
public sealed class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    /// <summary>
    /// Root directory for stored payloads. A relative value is resolved against the content root.
    /// Keep it OUTSIDE the web root: anything the web server can serve directly bypasses every
    /// permission check in the application.
    /// </summary>
    public string RootPath { get; set; } = "App_Data/documents";

    /// <summary>
    /// Largest upload accepted, in bytes. The limit exists so a single request cannot fill the
    /// disk; it is enforced while streaming, not from the declared content length, which a
    /// client controls.
    /// </summary>
    public long MaxSizeBytes { get; set; } = 25 * 1024 * 1024;
}

/// <summary>
/// Stores document payloads on the local file system.
/// <para>
/// Layout is <c>{root}/{tenant}/{aa}/{bb}/{storageName}</c>. The two nested levels come from the
/// first characters of the GUID: a single directory holding hundreds of thousands of files is
/// slow to enumerate on every common file system, and this keeps each one small without any
/// bookkeeping.
/// </para>
/// <para>
/// <b>Path safety.</b> The only variable part of the path is the GUID key the system generated,
/// and it is validated against that shape before use. Every resolved path is then checked to be
/// inside the configured root, so even a malformed key cannot reach another directory. The
/// original file name never touches the path — it lives in the database and is only used to name
/// the download.
/// </para>
/// </summary>
public sealed class FileSystemDocumentStorage : IDocumentStorage
{
    private readonly string _root;
    private readonly long _maxSizeBytes;
    private readonly ILogger<FileSystemDocumentStorage> _logger;

    public FileSystemDocumentStorage(
        IOptions<DocumentStorageOptions> options,
        IHostEnvironment environment,
        ILogger<FileSystemDocumentStorage> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        _logger = logger;
        _maxSizeBytes = options.Value.MaxSizeBytes;

        var configured = options.Value.RootPath;
        _root = Path.GetFullPath(
            Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(environment.ContentRootPath, configured));

        Directory.CreateDirectory(_root);
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(
        string storageName,
        int? tenantId,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var relativePath = BuildRelativePath(storageName, tenantId);
        var fullPath = ResolveInsideRoot(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        try
        {
            await using var target = new FileStream(
                fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true);

            await CopyWithLimitAsync(content, target, cancellationToken);
        }
        catch
        {
            // A partially written file is worse than none: the metadata row would point at
            // truncated content that looks valid.
            TryDelete(fullPath);
            throw;
        }

        return relativePath;
    }

    /// <inheritdoc />
    public Task<Stream?> OpenAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = ResolveInsideRoot(relativePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning(
                "Document payload is missing from storage: {RelativePath}", relativePath);
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TryDelete(ResolveInsideRoot(relativePath));
        return Task.CompletedTask;
    }

    // ------------------------------------------------------------------ internals

    /// <summary>
    /// Copies the payload while enforcing the size limit as the bytes arrive.
    /// <para>
    /// The declared <c>Content-Length</c> is not trusted: a client can understate it, so the
    /// count that matters is the one measured here.
    /// </para>
    /// </summary>
    private async Task CopyWithLimitAsync(Stream source, Stream target, CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long written = 0;

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            written += read;
            if (written > _maxSizeBytes)
            {
                throw new BusinessException(
                        "The file is larger than the allowed upload size.",
                        "Ensa:Document:FileTooLarge")
                    .WithData("MaxSizeBytes", _maxSizeBytes);
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    /// <summary>
    /// <c>{tenant}/{aa}/{bb}/{storageName}</c>, with the key validated as a bare GUID first.
    /// </summary>
    private static string BuildRelativePath(string storageName, int? tenantId)
    {
        if (!Guid.TryParseExact(storageName, "N", out _))
        {
            // The key is generated by the application; anything else means a caller built it,
            // which is exactly the input that must never reach a path.
            throw new BusinessException(
                "The document storage key is not valid.",
                "Ensa:Document:InvalidStorageKey");
        }

        var tenant = tenantId?.ToString() ?? "host";

        return string.Join('/', tenant, storageName[..2], storageName[2..4], storageName);
    }

    /// <summary>
    /// Resolves a relative path against the root and refuses anything that escapes it.
    /// <para>
    /// Belt and braces: the key is already validated, but a stored path could have been written
    /// by an older version or edited in the database, and a traversal must fail closed.
    /// </para>
    /// </summary>
    private string ResolveInsideRoot(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new BusinessException(
                "The document storage path is not valid.",
                "Ensa:Document:InvalidStorageKey");
        }

        var candidate = Path.GetFullPath(Path.Combine(_root, relativePath));

        if (!candidate.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !string.Equals(candidate, _root, StringComparison.Ordinal))
        {
            throw new BusinessException(
                "The document storage path is not valid.",
                "Ensa:Document:InvalidStorageKey");
        }

        return candidate;
    }

    private void TryDelete(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (IOException exception)
        {
            // Deletion is best-effort; a locked file must not fail the request that triggered it.
            _logger.LogWarning(exception, "Document payload could not be deleted: {Path}", fullPath);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Document payload could not be deleted: {Path}", fullPath);
        }
    }
}
