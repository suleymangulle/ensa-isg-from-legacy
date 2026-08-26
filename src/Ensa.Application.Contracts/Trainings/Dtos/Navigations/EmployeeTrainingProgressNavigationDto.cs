using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Trainings.Dtos.Navigations;

/// <summary>
/// Combined view of an employee's training progress: the progress record, the employee,
/// the training and the remaining time computed from the mandatory duration.
/// </summary>
public class EmployeeTrainingProgressNavigationDto : NavigationDto
{
    public EmployeeTrainingProgressDto Progress { get; set; } = null!;

    /// <summary>Employee taking the training, reduced to a lookup.</summary>
    public LookupDto? Employee { get; set; }

    /// <summary>Training being taken, reduced to a lookup.</summary>
    public LookupDto? Training { get; set; }

    /// <summary>
    /// Mandatory duration minus elapsed duration, in seconds; never negative.
    /// The mandatory duration comes from <c>ITrainingPlanningManager</c>.
    /// </summary>
    public int RemainingDurationSeconds { get; set; }
}
