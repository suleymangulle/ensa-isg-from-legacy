using Ensa.Domain.Common;

namespace Ensa.Domain.Trainings;

/// <summary>
/// A single question belonging to an exam.
/// <para>Legacy equivalent: <c>Sorular_T</c> (PK: <c>SoruId</c>).</para>
/// <para>
/// The legacy <c>CorrectAnswer</c> column held the correct answer as free text. After
/// normalization the correct option is marked with <see cref="ExamAnswer.IsCorrect"/>; this column
/// is kept for backward compatibility only — <c>ExamAnswer</c> is now the source of truth.
/// </para>
/// </summary>
public class ExamQuestion : FullAuditedTenantEntity, IActivatable
{
    public int ExamId { get; set; }

    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The legacy free-text correct answer, kept for backward compatibility.
    /// In the new flow the correct option is determined by <see cref="ExamAnswer.IsCorrect"/>.
    /// </summary>
    public string? CorrectAnswer { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
