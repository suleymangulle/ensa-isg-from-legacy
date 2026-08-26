using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Companies;

namespace Ensa.Domain.Risks.Navigations;

/// <summary>
/// Combined view of a corrective action with its company, documents and source field observation
/// line.
/// </summary>
[NotMapped]
public class CorrectiveActionNavigation : NavigationEntity
{
    /// <summary>The root corrective action record.</summary>
    public CorrectiveAction CorrectiveAction { get; set; } = null!;

    /// <summary>Summary of the company the action was raised for.</summary>
    public Company? Company { get; set; }

    /// <summary>The company employee responsible.</summary>
    public CompanyEmployee? OwnerEmployee { get; set; }

    /// <summary>Document for the finding stage (legacy <c>TDocumentId</c>).</summary>
    public Document? FindingDocument { get; set; }

    /// <summary>Document for the closing stage (legacy <c>SDocumentID</c>).</summary>
    public Document? ResultDocument { get; set; }

    /// <summary>The source line, when the action was derived from a field observation line.</summary>
    public FieldObservationLine? SourceFieldObservationLine { get; set; }
}
