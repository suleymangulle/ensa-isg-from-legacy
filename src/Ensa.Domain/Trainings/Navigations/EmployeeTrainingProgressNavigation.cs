using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Companies;

namespace Ensa.Domain.Trainings.Navigations;

/// <summary>
/// Combined view of an employee's training progress with the employee and the training.
/// <para>
/// <c>[NotMapped]</c> — NEVER a <c>DbSet</c>, never added to <c>ModelBuilder</c>;
/// populated in the repository layer through an <c>IQueryable</c> join and projection.
/// </para>
/// <para>
/// NORMALIZATION: the legacy computed field
/// <c>EmployeeTrainingProgressStatus_T.[NotMapped] RemainingDuration</c> was moved here
/// (<see cref="RemainingDurationSeconds"/>).
/// </para>
/// </summary>
[NotMapped]
public class EmployeeTrainingProgressNavigation : NavigationEntity<EmployeeTrainingProgress>
{
    public EmployeeTrainingProgress Progress
    {
        get => Entity;
        set => Entity = value;
    }

    public CompanyEmployee? Employee { get; set; }

    public Training? Training { get; set; }

    /// <summary>
    /// The remaining duration, computed as the training's total mandatory duration minus
    /// <see cref="EmployeeTrainingProgress.ElapsedDurationSeconds"/>. (Legacy: <c>[NotMapped] KalanSure</c>)
    /// </summary>
    public int RemainingDurationSeconds { get; set; }
}
