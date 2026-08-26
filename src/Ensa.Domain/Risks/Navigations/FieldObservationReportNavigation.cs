using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Companies;

namespace Ensa.Domain.Risks.Navigations;

/// <summary>
/// Combined view of a field observation report with its lines and related records.
/// </summary>
[NotMapped]
public class FieldObservationReportNavigation : NavigationEntity
{
    /// <summary>The root report record.</summary>
    public FieldObservationReport Report { get; set; } = null!;

    /// <summary>Summary of the company the observation was made at.</summary>
    public Company? Company { get; set; }

    /// <summary>The workplace department the observation was made in.</summary>
    public WorkplaceDepartment? Department { get; set; }

    /// <summary>The non-conformity lines on the report.</summary>
    public List<FieldObservationLineNavigation> Lines { get; set; } = [];
}

/// <summary>Combined view of a field observation line with its document and responsible employee.</summary>
[NotMapped]
public class FieldObservationLineNavigation : NavigationEntity
{
    /// <summary>The root line record.</summary>
    public FieldObservationLine Line { get; set; } = null!;

    /// <summary>Photograph or document evidencing the non-conformity (the replacement for the legacy <c>byte[] Document</c> triple).</summary>
    public Document? Document { get; set; }

    /// <summary>The company employee responsible for the line.</summary>
    public CompanyEmployee? OwnerEmployee { get; set; }

    /// <summary>Corrective actions derived from this line (<c>CorrectiveAction.FieldObservationLineId</c>).</summary>
    public List<CorrectiveAction> CorrectiveActions { get; set; } = [];
}
