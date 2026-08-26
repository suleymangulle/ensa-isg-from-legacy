using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Trainings;

/// <summary>
/// The answer a company employee gave to an exam question — a movement record.
/// <para>Legacy equivalent: <c>EmployeeSoruAnswer_T</c>.</para>
/// </summary>
public class EmployeeExamAnswer : CreationAuditedTenantEntity
{
    public int CompanyEmployeeId { get; set; }

    /// <summary>(Legacy: <c>SoruId</c>)</summary>
    public int ExamQuestionId { get; set; }

    /// <summary>The answer text the employee gave. (Legacy: <c>Cevap</c>)</summary>
    public string? Answer { get; set; }

    /// <summary>Whether the answer is correct. (Legacy: <c>Durum</c>)</summary>
    public bool IsCorrect { get; set; }

    /// <summary>(Legacy: <c>IlerlemeDurumId</c>)</summary>
    public int EmployeeTrainingProgressId { get; set; }

    /// <summary>(Legacy: <c>TestTip</c> int)</summary>
    public ExamAttemptType TestType { get; set; }

    public DateTime CevaplanmaDate { get; set; }
}
