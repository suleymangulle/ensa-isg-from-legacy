using Ensa.Domain.Common;

namespace Ensa.Domain.Trainings;

/// <summary>
/// A single answer option for an exam question.
/// <para>Legacy equivalent: <c>Cevaplar_T</c> (PK: <c>AnswerId</c>).</para>
/// <para>
/// NORMALIZATION (NEW FIELD): <see cref="IsCorrect"/>. Legacy held the correct answer only as the
/// free text in <see cref="ExamQuestion.CorrectAnswer"/> and never marked it among the options.
/// From now on the correct option is marked here.
/// </para>
/// </summary>
public class ExamAnswer : FullAuditedTenantEntity
{
    public int ExamQuestionId { get; set; }

    /// <summary>(Legacy: <c>Cevap_Metni</c>)</summary>
    public string AnswerText { get; set; } = string.Empty;

    /// <summary>Whether this option is the correct answer. (NEW field)</summary>
    public bool IsCorrect { get; set; }
}
