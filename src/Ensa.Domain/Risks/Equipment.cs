using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// Work equipment subject to periodic inspection: machinery, installations, lifting and conveying
/// gear, pressure vessels and so on.
/// (Legacy: <c>Cihaz_T</c> — the term "Cihaz" was replaced with "Ekipman" to match the wording of
/// the regulations.)
/// <para>
/// Conversions: <c>EquipmentType</c> string ("makine-tezgah", "tesisat-techizat", …) → the
/// <see cref="EquipmentType"/> enum; <c>byte[] PerExaminationReportDocument</c> →
/// <see cref="ExaminationReportDocumentId"/>; <c>IsDeleted</c> → the base class;
/// <c>OrganizationId</c> → <c>TenantId</c>.
/// </para>
/// </summary>
public class Equipment : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The company the equipment is located at. FK → <c>Company.Id</c>.</summary>
    public int CompanyId { get; set; }

    /// <summary>Name of the equipment. (Legacy: <c>CihazAdi</c>)</summary>
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>Equipment type. (Legacy: <c>CihazTuru</c> string)</summary>
    public EquipmentType EquipmentType { get; set; } = EquipmentType.Unspecified;

    // ---- Periodic inspection ----

    /// <summary>Textual summary or number of the periodic inspection report. (Legacy: <c>PerMuayeneRaporu</c>)</summary>
    public string? ExaminationReport { get; set; }

    /// <summary>Periyodik muayene raporu belgesi. FK → <c>Document.Id</c>. (Legacy: <c>byte[] PerMuayeneRaporuDosya</c>)</summary>
    public int? ExaminationReportDocumentId { get; set; }

    /// <summary>The person or body that carried out the inspection. (Legacy: <c>PerMuayeneYapan</c>)</summary>
    public string? ExaminationPerformedBy { get; set; }

    /// <summary>Date of the last periodic inspection.</summary>
    public DateTime? ExaminationDate { get; set; }

    /// <summary>
    /// The next inspection date, computed as <see cref="ExaminationDate"/> plus <c>Period</c>.
    /// <c>IEquipmentRepository.GetExaminationOverdueAsync</c> queries on this field.
    /// </summary>
    public DateTime? NextExaminationDate { get; set; }

    /// <summary>Muayene periyodu. FK → <c>Period.Id</c>. (Legacy: <c>PeriyotId</c>)</summary>
    public int? PeriodId { get; set; }

    /// <summary>
    /// Whether a user may delete the record. (Legacy: <c>Deletable</c>)
    /// It is <c>false</c> for records created automatically or by an integration.
    /// </summary>
    public bool Deletable { get; set; } = true;
}
