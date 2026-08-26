using Ensa.Domain.Common;

namespace Ensa.Domain.Trainings;

/// <summary>
/// A company employee's distance-learning progress in a training, optionally scoped to a single
/// topic.
/// <para>Legacy equivalent: <c>EmployeeTrainingProgressStatus_T</c> (PK: <c>ProgressStatusId</c>).</para>
/// <para>
/// NORMALIZATION: the legacy computed field <c>[NotMapped] RemainingDuration</c> was removed from
/// the entity and moved to
/// <see cref="Navigations.EmployeeTrainingProgressNavigation.RemainingDurationSeconds"/>.
/// </para>
/// <para>
/// SOFT DELETE: this row is the evidence that an employee completed a statutory training, so it
/// must survive deletion the way every other Trainings entity does. It used to derive from
/// <c>AuditedTenantEntity</c>, which has no <c>IsDeleted</c> flag, so a delete destroyed the record.
/// </para>
/// </summary>
public class EmployeeTrainingProgress : FullAuditedTenantEntity, IActivatable
{
    /// <summary>(Legacy: <c>PersonelId</c>)</summary>
    public int CompanyEmployeeId { get; set; }

    public int TrainingId { get; set; }

    /// <summary>The topic the progress belongs to, when progress is tracked per topic. (Legacy: <c>KonuId</c>)</summary>
    public int? TrainingTopicId { get; set; }

    /// <summary>(Legacy: <c>IlkTestDurum</c>)</summary>
    public bool FirstTestCompleted { get; set; }

    public int? FirstTestNote { get; set; }

    /// <summary>(Legacy: <c>SonTestDurum</c>)</summary>
    public bool LatestTestCompleted { get; set; }

    public int? LatestTestNote { get; set; }

    /// <summary>Total time spent on the training. (Legacy: <c>GecenSure</c>)</summary>
    public int ElapsedDurationSeconds { get; set; }

    public int ActivePage { get; set; }

    /// <summary>(Legacy: <c>EgitimUzmanId</c>)</summary>
    public int? TrainingSpecialistUserId { get; set; }

    /// <summary>(Legacy: <c>EgitimHekimId</c>)</summary>
    public int? TrainingPhysicianUserId { get; set; }

    /// <summary>(Legacy: <c>Aktif</c> bool?)</summary>
    public bool IsActive { get; set; } = true;
}
