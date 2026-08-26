using Ensa.Domain.Common;

namespace Ensa.Domain.Trainings;

/// <summary>
/// Link table mapping a training to an exam.
/// <para>Legacy equivalent: <c>TopicTest_T</c> — despite the name, its legacy FK was
/// <c>TrainingId</c>, so the mapping was per training rather than per topic; the naming was
/// corrected to match.</para>
/// </summary>
public class TrainingExam : FullAuditedTenantEntity, IActivatable
{
    public int TrainingId { get; set; }

    public int ExamId { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
