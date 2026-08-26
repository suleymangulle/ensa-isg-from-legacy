using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Risks.Dtos.Navigations;

/// <summary>
/// Incident combined with its department, document and person lists.
/// Mirrors <c>Ensa.Domain.Risks.Navigations.IncidentNavigation</c>.
/// </summary>
public class IncidentNavigationDto : NavigationDto
{
    /// <summary>Root incident record.</summary>
    public IncidentDto Incident { get; set; } = null!;

    /// <summary>Company the incident occurred at.</summary>
    public LookupDto? Company { get; set; }

    /// <summary>Workplace department the incident occurred in.</summary>
    public LookupDto? Department { get; set; }

    /// <summary>Incident report / photo document.</summary>
    public LookupDto? Document { get; set; }

    /// <summary>Supervisor of the unit where the incident occurred.</summary>
    public LookupDto? UnitSupervisor { get; set; }

    /// <summary>People affected by the incident.</summary>
    public List<IncidentPersonDto> AffectedPersons { get; set; } = [];

    /// <summary>People who witnessed the incident.</summary>
    public List<IncidentPersonDto> WitnessPersons { get; set; } = [];

    /// <summary>People who responded to the incident.</summary>
    public List<IncidentPersonDto> ResponderPersons { get; set; } = [];
}
