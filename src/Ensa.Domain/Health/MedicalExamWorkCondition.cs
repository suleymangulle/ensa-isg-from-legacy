using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Health;

/// <summary>
/// "Is the employee fit to work under this condition?" assessment line.
/// <para>
/// NORMALISATION: this replaces the encrypted string columns <c>HighCalis</c>,
/// <c>NightCalis</c>, <c>ShiftCalis</c>, <c>WorkCondition</c> and <c>BedenMentally</c> of the
/// legacy <c>PeriodicExaminationForm_T</c>.
/// </para>
/// <para>
/// Unique constraint: (<see cref="MedicalExaminationFormId"/>, <see cref="ConditionType"/>).
/// </para>
/// </summary>
public class MedicalExamWorkCondition : FullAuditedTenantEntity
{
    public int MedicalExaminationFormId { get; set; }

    /// <summary>The working condition that was assessed. (Legacy: the column name itself)</summary>
    public WorkConditionType ConditionType { get; set; }

    /// <summary>
    /// The physician's fitness opinion for this condition.
    /// (Legacy: the column's "Evet"/"Hayır"/empty string value)
    /// </summary>
    public TriStateAnswer Suitable { get; set; } = TriStateAnswer.Unspecified;
}
