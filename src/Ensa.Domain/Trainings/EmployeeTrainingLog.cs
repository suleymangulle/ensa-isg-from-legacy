using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Trainings;

/// <summary>
/// Audit log recording every action an employee takes in the distance-learning portal.
/// <para>Legacy equivalent: <c>EmployeeLoglamasi_T</c>.</para>
/// <para>The legacy <c>OperationDate</c> column is covered by the base class <c>CreationTime</c>.</para>
/// </summary>
public class EmployeeTrainingLog : CreationAuditedTenantEntity
{
    /// <summary>(Legacy: <c>PersonelId</c>)</summary>
    public int CompanyEmployeeId { get; set; }

    /// <summary>(Legacy: <c>Islem</c> — <c>EmployeeOperationEnum</c>)</summary>
    public EmployeeTrainingAction Operation { get; set; }

    /// <summary>(Legacy: <c>IslenenKonu</c>)</summary>
    public int? TrainingTopicId { get; set; }

    /// <summary>(Legacy: <c>IslenenSayfa</c>)</summary>
    public int? Page { get; set; }

    /// <summary>(Legacy: <c>GecenSure</c>)</summary>
    public int? ElapsedDurationSeconds { get; set; }

    /// <summary>(Legacy: <c>KalanSure</c>)</summary>
    public int? RemainingDurationSeconds { get; set; }

    /// <summary>(Legacy: <c>TestId</c>)</summary>
    public int? ExamId { get; set; }

    /// <summary>(Legacy: <c>SinavNotu</c>)</summary>
    public int? ExamNote { get; set; }

    /// <summary>(Legacy: <c>PersonelEgitimIlerlemeDurumId</c>)</summary>
    public int? EmployeeTrainingProgressId { get; set; }
}
