using Ensa.Domain.Common;

namespace Ensa.Domain.Health;

/// <summary>
/// Diagnosis (ICD-10) line of an e-prescription.
/// <para>Legacy equivalent: <c>EPrescriptionDiagnosis_T</c>.</para>
/// </summary>
public class EPrescriptionDiagnosis : FullAuditedTenantEntity
{
    public int EPrescriptionId { get; set; }

    /// <summary>
    /// ICD-10 code — this is the value written into the e-prescription XML and it is kept
    /// historically as it was at the time of submission. (Legacy: <c>ICD10_Kodu</c>)
    /// </summary>
    public string Icd10Code { get; set; } = string.Empty;

    /// <summary>
    /// NORMALISATION (new column): the SKRS ICD-10 reference (host table <see cref="Icd10"/>).
    /// The legacy system stored only the code text; this FK makes the diagnosis name and the
    /// hierarchy readable through a join. It stays <c>null</c> for legacy codes that are no
    /// longer in the reference list.
    /// </summary>
    public int? Icd10Id { get; set; }
}
