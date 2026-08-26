using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Risks.Dtos.Navigations;

/// <summary>
/// Field observation report combined with its department, lines and everything hanging off them.
/// Mirrors <c>Ensa.Domain.Risks.Navigations.FieldObservationReportNavigation</c>.
/// </summary>
public class FieldObservationReportNavigationDto : NavigationDto
{
    /// <summary>Root report record.</summary>
    public FieldObservationReportDto Report { get; set; } = null!;

    /// <summary>Company the observation was carried out at.</summary>
    public LookupDto? Company { get; set; }

    /// <summary>Workplace department the observation was carried out in.</summary>
    public LookupDto? Department { get; set; }

    /// <summary>Non-conformity lines of the report.</summary>
    public List<FieldObservationLineNavigationDto> Lines { get; set; } = [];
}

/// <summary>Observation line together with its document, responsible employee and derived actions.</summary>
public class FieldObservationLineNavigationDto : NavigationDto
{
    /// <summary>Root line record.</summary>
    public FieldObservationLineDto Line { get; set; } = null!;

    /// <summary>Photo / document attached to the non-conformity.</summary>
    public LookupDto? Document { get; set; }

    /// <summary>Company employee responsible for the line.</summary>
    public LookupDto? OwnerEmployee { get; set; }

    /// <summary>Corrective actions derived from this line.</summary>
    public List<CorrectiveActionDto> CorrectiveActions { get; set; } = [];
}
