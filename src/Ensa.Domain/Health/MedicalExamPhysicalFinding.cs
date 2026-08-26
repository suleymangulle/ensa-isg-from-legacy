using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Health;

/// <summary>
/// Physical examination finding line of an examination form (one per body system).
/// <para>
/// NORMALISATION: this replaces the 12 separate encrypted string columns of the legacy
/// <c>PeriodicExaminationForm_T</c> (<c>SensoryEye</c>, <c>SensoryEarNoseThroat</c>,
/// <c>SensorySkin</c>, <c>CardiovascularSisMu</c>, <c>RespiratorySisMu</c>,
/// <c>DigestiveSisMu</c>, <c>UrogenitalSisMu</c>, <c>MuscularSkeletalSisMu</c>,
/// <c>NeurologicalMu</c>, <c>PiskiyatrikMu</c>, <c>FizikMuOther</c>).
/// </para>
/// <para>
/// Unique constraint: (<see cref="MedicalExaminationFormId"/>, <see cref="System"/>).
/// </para>
/// </summary>
public class MedicalExamPhysicalFinding : FullAuditedTenantEntity
{
    public int MedicalExaminationFormId { get; set; }

    /// <summary>The system/organ group that was assessed. (Legacy: the column name itself)</summary>
    public PhysicalExamSystem System { get; set; }

    /// <summary>The finding. (Legacy: the column's "Normal"/"Patolojik" string value)</summary>
    public ExamFinding Finding { get; set; } = ExamFinding.Unspecified;

    /// <summary>
    /// Detail of a pathological finding, or the legacy free text. ENCRYPTED COLUMN.
    /// Mandatory for <see cref="PhysicalExamSystem.Other"/>.
    /// </summary>
    public string? Description { get; set; }
}
