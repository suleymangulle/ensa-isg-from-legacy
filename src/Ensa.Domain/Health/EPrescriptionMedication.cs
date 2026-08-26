using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Health;

/// <summary>
/// Medication line of an e-prescription.
/// <para>Legacy equivalent: <c>EPrescriptionMedication_T</c>.</para>
/// <para>
/// SKRS references are linked by FK only (<see cref="MedicationId"/>,
/// <see cref="UsageMethodId"/>, <see cref="UsagePeriodUnitId"/>,
/// <see cref="UsageDoseUnitId"/>); there are NO navigation properties.
/// </para>
/// </summary>
public class EPrescriptionMedication : FullAuditedTenantEntity
{
    public int EPrescriptionId { get; set; }

    /// <summary>
    /// SKRS medication reference (host table <see cref="Medication"/>).
    /// (Legacy: <c>SKRS_IlacId</c>)
    /// </summary>
    public int MedicationId { get; set; }

    /// <summary>
    /// Barcode of the medication — a historical copy of the value at the time of submission.
    /// (Legacy: <c>IlacBarkodu</c>)
    /// </summary>
    public string? MedicationBarcode { get; set; }

    /// <summary>
    /// Administration route reference (host table <see cref="MedicationRoute"/>).
    /// (Legacy: <c>SKRS_KullanimSekliId</c>)
    /// </summary>
    public int UsageMethodId { get; set; }

    /// <summary>
    /// Dose unit reference (host table <see cref="MedicationDoseUnit"/>).
    /// (Legacy: <c>SKRS_IlacKullanimDozBirimiId</c>)
    /// </summary>
    public int UsageDoseUnitId { get; set; }

    /// <summary>
    /// Frequency unit reference (host table <see cref="MedicationFrequencyUnit"/>).
    /// (Legacy: <c>SKRS_IlacKullanimPeriyoduBirimiId</c>)
    /// </summary>
    public int UsagePeriodUnitId { get; set; }

    /// <summary>Number of boxes. (Legacy: <c>Kutu</c>)</summary>
    public int Box { get; set; }

    /// <summary>Dose amount (integer part). (Legacy: <c>Doz</c>)</summary>
    public int Dose { get; set; }

    /// <summary>
    /// Fractional dose amount (e.g. 0.5 tablet). (Legacy: <c>Doz2</c> <c>double?</c>)
    /// The legacy <c>double</c> was converted to <c>decimal</c>.
    /// </summary>
    public decimal? DoseFraction { get; set; }

    /// <summary>Usage period (expressed in <see cref="UsagePeriodUnitId"/>). (Legacy: <c>Periyot</c>)</summary>
    public int Period { get; set; }

    /// <summary>Note specific to this medication. ENCRYPTED COLUMN. (Legacy: <c>IlacAciklama</c>)</summary>
    public string? MedicationDescription { get; set; }

    /// <summary>Type of the medication note. (Legacy: <c>IlacAciklamaTuru</c> int? — a magic int)</summary>
    public PrescriptionNoteType MedicationDescriptionType { get; set; } = PrescriptionNoteType.Unspecified;
}
