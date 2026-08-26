using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Health;

/// <summary>
/// Anamnesis/complaint line of an examination form.
/// <para>
/// NORMALISATION: this replaces the ~23 separate encrypted string columns of the legacy
/// <c>PeriodicExaminationForm_T</c> (<c>ProductiveCough</c>, <c>BreathShortness</c>,
/// <c>ChestPain</c>, <c>Palpitation</c>, <c>BackPain</c>, <c>DiarrheaOrConstipation</c>,
/// <c>JointPain</c>, <c>CardiacDisease</c>, <c>DiabetesDisease</c>, <c>RenalDisease</c>,
/// <c>Jaundice</c>, <c>GastricOrOnTwoDuodenalUlcer</c>, <c>HearingLoss</c>,
/// <c>VisionImpairment</c>, <c>NervousSystemDisease</c>, <c>SkinDisease</c>,
/// <c>FoodPoisoning</c>, <c>HospitalYattinizMi</c>, <c>SurgeryGecirdinizMi</c>,
/// <c>WorkAccidentGecirdinizMi</c>, <c>MesHasSupMu</c>, <c>DisabilityAldinizMi</c>,
/// <c>TreatmentGoruyorMusunuz</c>).
/// </para>
/// <para>
/// A form carries at most one row per complaint type:
/// unique constraint (<see cref="MedicalExaminationFormId"/>, <see cref="ComplaintType"/>).
/// </para>
/// </summary>
public class MedicalExamComplaint : FullAuditedTenantEntity
{
    public int MedicalExaminationFormId { get; set; }

    /// <summary>Complaint/history heading. (Legacy: the column name itself)</summary>
    public MedicalComplaintType ComplaintType { get; set; }

    /// <summary>
    /// The employee's answer. (Legacy: the column's "Evet"/"Hayır"/empty string value)
    /// </summary>
    public TriStateAnswer Answer { get; set; } = TriStateAnswer.Unspecified;

    /// <summary>Free-text note (date, diagnosis name, duration, ...). ENCRYPTED COLUMN.</summary>
    public string? Description { get; set; }
}
