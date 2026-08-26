using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Risks.Dtos.Navigations;

/// <summary>
/// Corrective action combined with its company, owner, documents and source observation line.
/// Mirrors <c>Ensa.Domain.Risks.Navigations.CorrectiveActionNavigation</c>.
/// </summary>
public class CorrectiveActionNavigationDto : NavigationDto
{
    /// <summary>Root corrective action record.</summary>
    public CorrectiveActionDto CorrectiveAction { get; set; } = null!;

    /// <summary>Company the action was opened for.</summary>
    public LookupDto? Company { get; set; }

    /// <summary>Responsible company employee.</summary>
    public LookupDto? OwnerEmployee { get; set; }

    /// <summary>Document attached to the finding stage.</summary>
    public LookupDto? FindingDocument { get; set; }

    /// <summary>Document attached to the result stage.</summary>
    public LookupDto? ResultDocument { get; set; }

    /// <summary>Source field observation line, when the action was derived from one.</summary>
    public FieldObservationLineDto? SourceFieldObservationLine { get; set; }
}
