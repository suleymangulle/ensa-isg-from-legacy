using Ensa.Domain.Common;

namespace Ensa.Domain.Documents;

/// <summary>
/// A single office-scoped document record under a <see cref="ModuleArchive"/> header.
/// <para>Legacy equivalent: <c>ModuleArchiveDetail_T</c>.</para>
/// <para>
/// The <c>byte[] Document</c> + <c>DocumentName</c> + <c>DocumentType</c> + <c>DocumentBoyutu</c>
/// quadruple was normalized into the central <c>Document</c> table (<see cref="DocumentId"/>);
/// name, type and size are now read from the <c>Document</c> record.
/// </para>
/// </summary>
public class ModuleArchiveItem : AuditedTenantEntity
{
    /// <summary>The archive header this item belongs to. FK — no navigation property.</summary>
    public int ModuleArchiveId { get; set; }

    /// <summary>The related office. FK — no navigation property.</summary>
    public int OfficeId { get; set; }

    /// <summary>FK to the central <c>Document</c> table.</summary>
    public int DocumentId { get; set; }
}
