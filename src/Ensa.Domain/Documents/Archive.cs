using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Documents;

/// <summary>
/// A module-scoped archive record: it states which module, line and month a document produced by
/// an activity (a monthly inspection, a training session, and so on) belongs to.
/// <para>Legacy equivalent: <c>Archive_T</c>.</para>
/// <para>
/// The <c>byte[] Document</c> + <c>DocumentName</c> + <c>DocumentType</c> triple was normalized
/// into the central <c>Document</c> table (<see cref="DocumentId"/>). The free-text
/// <c>MonthYazi</c> column was REMOVED; the month name is derived from the <see cref="Month"/>
/// integer where needed.
/// </para>
/// </summary>
public class Archive : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The module the document belongs to. (Legacy: <c>Modul</c> string)</summary>
    public DocumentOwnerType ModuleType { get; set; } = DocumentOwnerType.Unspecified;

    /// <summary>Id of the related record inside that module.</summary>
    public int ModuleId { get; set; }

    /// <summary>FK to the central <c>Document</c> table.</summary>
    public int DocumentId { get; set; }

    /// <summary>The related company. FK — no navigation property.</summary>
    public int CompanyId { get; set; }

    /// <summary>Reference to a child line inside that module, if any.</summary>
    public int? LineId { get; set; }

    /// <summary>The month the archive entry belongs to (1-12).</summary>
    public int? Month { get; set; }

    /// <summary>The year the archive entry belongs to.</summary>
    public int? Year { get; set; }

    /// <summary>Free-text note.</summary>
    public string? Description { get; set; }

    /// <summary>Module-specific free-text note; legacy kept this separate from <c>Description</c>.</summary>
    public string? ModuleDescription { get; set; }

    /// <summary>
    /// The original creation date carried over from the pre-migration system, kept so that no
    /// information is lost during data migration. (Legacy: <c>EskiEklemeTarihi</c>)
    /// </summary>
    public DateTime? PreviousAddDate { get; set; }

    /// <summary>Reference to the user who added the record in the old system. (Legacy: <c>EskiEkleyenKullanici</c>)</summary>
    public int? PreviousAddedByUserId { get; set; }
}
