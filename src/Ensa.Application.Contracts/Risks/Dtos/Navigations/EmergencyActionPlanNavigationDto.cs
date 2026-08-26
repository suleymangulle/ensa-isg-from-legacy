using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Risks.Dtos.Navigations;

/// <summary>
/// Emergency action plan combined with its sections, team members and documents.
/// Mirrors <c>Ensa.Domain.Risks.Navigations.EmergencyActionPlanNavigation</c>.
/// </summary>
public class EmergencyActionPlanNavigationDto : NavigationDto
{
    /// <summary>Root plan record.</summary>
    public EmergencyActionPlanDto Plan { get; set; } = null!;

    /// <summary>Company the plan belongs to.</summary>
    public LookupDto? Company { get; set; }

    /// <summary>Evacuation layout drawing.</summary>
    public LookupDto? EvacuationPlanDocument { get; set; }

    /// <summary>Signed / PDF copy of the plan.</summary>
    public LookupDto? Document { get; set; }

    /// <summary>Free-text sections of the plan, ordered by <c>OrderNo</c>.</summary>
    public List<EmergencyPlanSectionDto> Sections { get; set; } = [];

    /// <summary>Members assigned to the emergency teams.</summary>
    public List<EmergencyTeamMemberNavigationDto> TeamMembers { get; set; } = [];
}

/// <summary>Emergency team member together with the employee summary.</summary>
public class EmergencyTeamMemberNavigationDto : NavigationDto
{
    /// <summary>Root team member record.</summary>
    public EmergencyTeamMemberDto TeamMember { get; set; } = null!;

    /// <summary>Assigned company employee.</summary>
    public LookupDto? Employee { get; set; }
}
