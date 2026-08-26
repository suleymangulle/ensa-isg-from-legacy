using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Membership;

namespace Ensa.Domain.Plans.Navigations;

/// <summary>
/// Combined view of a work plan with its company, OHS specialist, physician and plan lines,
/// including activity names and evidence documents.
/// <para>
/// <c>[NotMapped]</c> — NEVER a <c>DbSet</c>, never added to <c>ModelBuilder</c>;
/// populated in the repository layer through an <c>IQueryable</c> join and projection.
/// </para>
/// </summary>
[NotMapped]
public class WorkPlanNavigation : NavigationEntity<WorkPlan>
{
    public WorkPlan WorkPlan
    {
        get => Entity;
        set => Entity = value;
    }

    public Company? Company { get; set; }

    public User? Specialist { get; set; }

    public User? Physician { get; set; }

    public User? Approver { get; set; }

    public List<WorkPlanLineNavigation> Lines { get; set; } = [];
}

/// <summary>
/// Combined view of a work plan line with its activity name, trainer user if any, and evidence
/// document.
/// </summary>
[NotMapped]
public class WorkPlanLineNavigation : NavigationEntity<WorkPlanLine>
{
    public WorkPlanLine WorkPlanLine
    {
        get => Entity;
        set => Entity = value;
    }

    /// <summary>Lookup — <see cref="Activity.ActivityName"/>.</summary>
    public string ActivityName { get; set; } = string.Empty;

    public User? InstructorUser { get; set; }

    /// <summary>
    /// Display name of the evidence document. It is a lookup: the <c>Document</c> table is defined in
    /// the Documents module, so only the name is projected here.
    /// </summary>
    public string? DocumentName { get; set; }
}
