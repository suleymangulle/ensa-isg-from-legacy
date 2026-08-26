using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Health;

/// <summary>
/// Header of an e-prescription issued by the workplace physician (a MEDULA / e-prescription
/// service record).
/// <para>Legacy equivalent: <c>EPrescription_T</c>.</para>
/// <para>
/// The prescription lines live in <see cref="EPrescriptionMedication"/> and the diagnoses in
/// <see cref="EPrescriptionDiagnosis"/>; there are NO navigation properties.
/// </para>
/// </summary>
public class EPrescription : FullAuditedTenantEntity
{
    /// <summary>
    /// Prescription code returned by the e-prescription service (populated after a successful
    /// submission). (Legacy: <c>EReceteKodu</c>)
    /// </summary>
    public string? EPrescriptionCode { get; set; }

    /// <summary>Hospital/facility protocol number. (Legacy: <c>ProtokolNo</c>)</summary>
    public string? ProtocolNo { get; set; }

    // ---------------- Patient ----------------

    /// <summary>
    /// The patient's national ID — a mandatory field on the e-prescription submission.
    /// ENCRYPTED COLUMN (personal data). (Legacy: <c>HastaTcKimlikNo</c>)
    /// </summary>
    public string PatientNationalId { get; set; } = string.Empty;

    /// <summary>
    /// NORMALISATION (new column): the <c>CompanyEmployee</c> record of the employee the
    /// prescription was issued for. In the legacy system a prescription only carried the national
    /// ID and was never linked to the employee record; this FK lets a prescription be attached to
    /// the health surveillance history. It stays <c>null</c> for external patients who have no
    /// employee record.
    /// </summary>
    public int? PatientCompanyEmployeeId { get; set; }

    // ---------------- Notes ----------------

    /// <summary>Prescription note (free text). ENCRYPTED COLUMN. (Legacy: <c>Aciklama</c>)</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of the note. (Legacy: <c>AciklamaTuru</c> int? — a magic int)
    /// </summary>
    public PrescriptionNoteType DescriptionType { get; set; } = PrescriptionNoteType.Unspecified;

    // ---------------- Submission result ----------------

    /// <summary>Whether the prescription was cancelled. (Legacy: <c>Iptal</c> bool?)</summary>
    public bool Cancelled { get; set; }

    /// <summary>Time it was submitted to the e-prescription service. (Legacy: <c>GonderimTarihi</c>)</summary>
    public DateTime? SubmissionDate { get; set; }

    /// <summary>Result code returned by the service. (Legacy: <c>SonucKodu</c>)</summary>
    public string? ResultCode { get; set; }

    /// <summary>Result message returned by the service. (Legacy: <c>SonucMesaji</c>)</summary>
    public string? ResultMessage { get; set; }

    /// <summary>Warning returned by the service (interaction, dosage warning, ...). (Legacy: <c>UyariMesaji</c>)</summary>
    public string? WarningMessage { get; set; }
}
