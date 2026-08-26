using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Trainings.Dtos.Navigations;

/// <summary>
/// Combined view of an annual training plan: the header, the workplace, the specialist and
/// physician who drew it up, and the plan lines enriched with training and document names.
/// </summary>
public class TrainingPlanNavigationDto : NavigationDto
{
    public TrainingPlanDto TrainingPlan { get; set; } = null!;

    /// <summary>Workplace the plan belongs to, reduced to a lookup.</summary>
    public LookupDto? Company { get; set; }

    /// <summary>Occupational safety specialist's display name.</summary>
    public string? SpecialistFullName { get; set; }

    /// <summary>Occupational physician's display name.</summary>
    public string? PhysicianFullName { get; set; }

    /// <summary>Approver's display name.</summary>
    public string? ApproverFullName { get; set; }

    public List<TrainingPlanLineNavigationDto> Lines { get; set; } = [];
}

/// <summary>A training plan line together with its resolved training, instructor and document names.</summary>
public class TrainingPlanLineNavigationDto : NavigationDto
{
    public TrainingPlanLineDto Line { get; set; } = null!;

    /// <summary>Name of the planned training.</summary>
    public string TrainingName { get; set; } = string.Empty;

    /// <summary>Instructor's display name when the instructor is a system user.</summary>
    public string? InstructorUserFullName { get; set; }

    /// <summary>Display name of the evidence document.</summary>
    public string? DocumentName { get; set; }
}
