using Ensa.Domain.Common;

namespace Ensa.Domain.Risks;

/// <summary>
/// A document or inspection certificate attached to a piece of equipment. (Legacy: <c>CihazEvrak_T</c>)
/// <para>
/// The legacy constructor that carried business logic
/// (<c>EquipmentDocument_T(int, string, ..., User_T)</c>) was REMOVED — creating the record and
/// filling the audit fields is the responsibility of the manager and application service layers.
/// </para>
/// </summary>
public class EquipmentDocument : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>FK → <see cref="Equipment"/>. (Legacy: <c>CihazId</c>)</summary>
    public int EquipmentId { get; set; }

    /// <summary>Denormalized company FK, present in legacy too and kept for query convenience. FK → <c>Company.Id</c>.</summary>
    public int CompanyId { get; set; }

    /// <summary>The document file. FK → <c>Document.Id</c>.</summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// Type of the document. FK → <see cref="EquipmentDocumentType"/>.
    /// Legacy had no FK between <c>EquipmentDocument_T</c> and <c>EquipmentDocumentList_T</c>; they
    /// were matched on the <c>Description</c> text. The FK was added for referential integrity.
    /// </summary>
    public int? EquipmentDocumentTypeId { get; set; }

    /// <summary>Description of the document. (Legacy: <c>Aciklama</c>)</summary>
    public string? Description { get; set; }

    /// <summary>The inspection date the document is based on.</summary>
    public DateTime? ExaminationDate { get; set; }

    /// <summary>End of the document's validity.</summary>
    public DateTime? ValidityDate { get; set; }

    /// <summary>The person or body that carried out the inspection. (Legacy: <c>MuayeneYapan</c>)</summary>
    public string? ExaminationPerformedBy { get; set; }

    /// <summary>The activity that produced the document. FK → <c>Activity.Id</c>. (Legacy: <c>AktiviteId</c>)</summary>
    public int? ActivityId { get; set; }

    /// <summary>The work plan line that produced the document. FK → <c>WorkPlanLine.Id</c>.</summary>
    public int? WorkPlanLineId { get; set; }
}
