using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Documents;

/// <summary>
/// The central document store — the single record for EVERY binary payload in the system.
/// <para>
/// This table is the central document store for every module. The <c>byte[] Document</c> +
/// <c>DocumentName</c> + <c>DocumentType</c> column triple that legacy repeated in table after
/// table (<c>Document_T</c>, <c>Archive_T</c>, <c>Form_T</c>, <c>ModuleArchiveDetail_T</c>,
/// <c>CompanyLogo</c> and dozens more) is normalized here. Every other module links its
/// content-bearing fields to this table through a <c>DocumentId</c> (<c>int</c>/<c>int?</c>) FK;
/// navigation properties are NOT used.
/// </para>
/// <para>Primary legacy equivalent: <c>Document_T</c>.</para>
/// </summary>
public class Document : FullAuditedTenantEntity, IActivatable, ICompanyScoped
{
    /// <summary>The category the document belongs to. FK — no navigation property.</summary>
    public int? DocumentCategoryId { get; set; }

    /// <summary>The company the document relates to, if any. FK — no navigation property.</summary>
    public int? CompanyId { get; set; }

    /// <summary>The original file name as the uploader sees it, extension included.</summary>
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// The unique, GUID-based storage name on disk or in blob storage. It is never shown to the
    /// user, and it removes both name collisions and the path-traversal risk.
    /// </summary>
    public string StorageName { get; set; } = string.Empty;

    /// <summary>File extension without the leading dot, e.g. "pdf".</summary>
    public string? Extension { get; set; }

    /// <summary>MIME content type, e.g. "application/pdf". (Legacy: <c>DosyaTuru</c>)</summary>
    public string? ContentType { get; set; }

    /// <summary>File size in bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Binary content held in the database for small files. Large files live on the file system or
    /// in blob storage under <see cref="StoragePath"/>, in which case <see cref="Content"/> is
    /// <c>null</c>.
    /// </summary>
    public byte[]? Content { get; set; }

    /// <summary>
    /// Relative storage path on the file system or in blob storage. Set whenever
    /// <see cref="Content"/> is not held in the database.
    /// </summary>
    public string? StoragePath { get; set; }

    /// <summary>SHA-256 digest of the content, used to detect duplicate files.</summary>
    public string? Sha256 { get; set; }

    /// <summary>The module or record type the document is polymorphically attached to.</summary>
    public DocumentOwnerType OwnerType { get; set; } = DocumentOwnerType.Unspecified;

    /// <summary>
    /// Polymorphic link: the id of the record in the table <see cref="OwnerType"/> points at. This
    /// is NOT a conventional FK — a single column references several tables.
    /// </summary>
    public int? OwnerRecordId { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
