using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Membership;

namespace Ensa.Domain.Trainings.Navigations;

/// <summary>
/// Combined view of a training plan with its company, OHS specialist, physician and plan lines,
/// including training names and evidence documents.
/// <para>
/// <c>[NotMapped]</c> — NEVER a <c>DbSet</c>, never added to <c>ModelBuilder</c>;
/// populated in the repository layer through an <c>IQueryable</c> join and projection.
/// </para>
/// </summary>
[NotMapped]
public class TrainingPlanNavigation : NavigationEntity<TrainingPlan>
{
    public TrainingPlan TrainingPlan
    {
        get => Entity;
        set => Entity = value;
    }

    public Company? Company { get; set; }

    public User? Specialist { get; set; }

    public User? Physician { get; set; }

    public User? Approver { get; set; }

    public List<TrainingPlanLineNavigation> Lines { get; set; } = [];
}

/// <summary>
/// Combined view of a training plan line with its training name, trainer user if any, and evidence
/// document.
/// </summary>
[NotMapped]
public class TrainingPlanLineNavigation : NavigationEntity<TrainingPlanLine>
{
    public TrainingPlanLine TrainingPlanLine
    {
        get => Entity;
        set => Entity = value;
    }

    /// <summary>Lookup — <see cref="Training.TrainingName"/>.</summary>
    public string TrainingName { get; set; } = string.Empty;

    public User? InstructorUser { get; set; }

    /// <summary>
    /// Display name of the evidence document. It is a lookup: the <c>Document</c> table is defined in
    /// the Documents module, so only the name is projected here.
    /// </summary>
    public string? DocumentName { get; set; }
}
