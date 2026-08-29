using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Trainings.Dtos;

/// <summary>
/// An employee's progress through one remote training (optionally tracked per topic).
/// </summary>
public class EmployeeTrainingProgressDto : AuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int CompanyEmployeeId { get; set; }

    public int TrainingId { get; set; }

    /// <summary>Topic the progress belongs to, when progress is tracked per topic.</summary>
    public int? TrainingTopicId { get; set; }

    public bool FirstTestCompleted { get; set; }

    public int? FirstTestNote { get; set; }

    public bool LatestTestCompleted { get; set; }

    public int? LatestTestNote { get; set; }

    /// <summary>Total time spent in the training, in seconds.</summary>
    public int ElapsedDurationSeconds { get; set; }

    /// <summary>Page the employee is currently on.</summary>
    public int ActivePage { get; set; }

    public int? TrainingSpecialistUserId { get; set; }

    public int? TrainingPhysicianUserId { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>Input used to start (or resume) an employee's remote training.</summary>
public class StartTrainingProgressDto
{
    [Range(1, int.MaxValue, ErrorMessage = "An employee must be selected.")]
    public int CompanyEmployeeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A training must be selected.")]
    public int TrainingId { get; set; }

    /// <summary>Topic to start, when progress is tracked per topic.</summary>
    public int? TrainingTopicId { get; set; }

    public int? TrainingSpecialistUserId { get; set; }

    public int? TrainingPhysicianUserId { get; set; }
}

/// <summary>Input used to record progress within a topic (elapsed time and current page).</summary>
public class SaveTopicProgressDto
{
    /// <summary>Topic the progress belongs to; omit when the training is tracked as a whole.</summary>
    public int? TrainingTopicId { get; set; }

    /// <summary>
    /// Total seconds spent so far. The value never decreases: a lower value than the one
    /// already recorded is ignored, which makes replayed browser events harmless.
    /// </summary>
    [Range(0, 1000000, ErrorMessage = "The elapsed duration must be between 0 and 1000000 seconds.")]
    public int ElapsedDurationSeconds { get; set; }

    [Range(0, 100000)]
    public int ActivePage { get; set; }
}

/// <summary>Input used to submit an exam attempt.</summary>
public class SubmitExamDto
{
    /// <summary>
    /// <c>true</c> for the pre-test taken before the training, <c>false</c> for the final test.
    /// </summary>
    public bool IsFirstTest { get; set; }

    [Range(0, 100, ErrorMessage = "The score must be between 0 and 100.")]
    public int Score { get; set; }

    /// <summary>Whether the attempt counts as completed (passed).</summary>
    public bool IsCompleted { get; set; } = true;
}

/// <summary>Filter for an employee's progress records.</summary>
public class GetEmployeeTrainingProgressListInput : PagedAndSortedRequestDto
{
    /// <summary>Restricts the list to one workplace.</summary>
    public int? CompanyId { get; set; }

    /// <summary>Restricts the list to one employee.</summary>
    public int? CompanyEmployeeId { get; set; }

    public int? TrainingId { get; set; }

    public bool? LatestTestCompleted { get; set; }

    public bool? IsActive { get; set; }
}
/// <summary>
/// One row of the cross-employee progress list.
/// <para>
/// Display names are resolved by the service with batched queries, so the row is ready to render
/// without a request per employee.
/// </para>
/// </summary>
public class EmployeeTrainingProgressListDto : EntityDto
{
    public int CompanyEmployeeId { get; set; }

    public string? EmployeeFullName { get; set; }

    public int? CompanyId { get; set; }

    public string? CompanyName { get; set; }

    public int TrainingId { get; set; }

    public string? TrainingName { get; set; }

    /// <summary>Whether the final test has been passed — the statutory completion signal.</summary>
    public bool LatestTestCompleted { get; set; }

    public int? LatestTestNote { get; set; }

    public int ElapsedDurationSeconds { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreationTime { get; set; }

    public DateTime? LastModificationTime { get; set; }
}
