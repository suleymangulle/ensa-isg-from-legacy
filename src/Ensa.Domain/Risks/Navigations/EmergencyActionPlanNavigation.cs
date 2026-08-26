using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Companies;

namespace Ensa.Domain.Risks.Navigations;

/// <summary>
/// Combined view of an emergency action plan with its sections, team members and documents.
/// </summary>
[NotMapped]
public class EmergencyActionPlanNavigation : NavigationEntity
{
    /// <summary>The root plan record.</summary>
    public EmergencyActionPlan Plan { get; set; } = null!;

    /// <summary>Summary of the company the plan belongs to.</summary>
    public Company? Company { get; set; }

    /// <summary>Evacuation plan drawing (legacy <c>byte[] EvacuationPlani</c>).</summary>
    public Document? EvacuationPlanDocument { get; set; }

    /// <summary>The signed or PDF copy of the plan (legacy <c>byte[] Document</c>).</summary>
    public Document? Document { get; set; }

    /// <summary>
    /// The plan's free-text sections — the replacement for the nine flat string columns in legacy —
    /// ordered by <c>OrderNo</c>.
    /// </summary>
    public List<EmergencyPlanSection> Sections { get; set; } = [];

    /// <summary>Team members assigned in the plan, with their employee details.</summary>
    public List<EmergencyTeamMemberNavigation> TeamMembers { get; set; } = [];
}

/// <summary>Combined view of an emergency team member and their employee summary.</summary>
[NotMapped]
public class EmergencyTeamMemberNavigation : NavigationEntity
{
    /// <summary>The root team member record.</summary>
    public EmergencyTeamMember TeamMember { get; set; } = null!;

    /// <summary>Summary of the assigned company employee.</summary>
    public CompanyEmployee? Employee { get; set; }
}
