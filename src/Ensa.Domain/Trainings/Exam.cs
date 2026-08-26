using Ensa.Domain.Common;

namespace Ensa.Domain.Trainings;

/// <summary>
/// The header of an exam. It is linked to a training through <see cref="TrainingExam"/>.
/// <para>Legacy equivalent: <c>Test_T</c>.</para>
/// </summary>
public class Exam : FullAuditedTenantEntity, IActivatable
{
    public string Title { get; set; } = string.Empty;

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
