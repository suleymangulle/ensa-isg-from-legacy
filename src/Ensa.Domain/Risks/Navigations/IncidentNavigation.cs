using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Companies;

namespace Ensa.Domain.Risks.Navigations;

/// <summary>
/// Combined view of an incident record.
/// <para>
/// This is where the <c>[NotMapped]</c> fields of the legacy <c>Incident_T</c>
/// (<c>AffectedPersons</c>, <c>WitnessPersons</c>,
/// <c>GDocument</c>/<c>GDocumentName</c>/<c>GDocumentType</c>, <c>DepartmentName</c>) now live; the
/// entity carries nothing but real columns.
/// </para>
/// </summary>
[NotMapped]
public class IncidentNavigation : NavigationEntity
{
    /// <summary>The root incident record.</summary>
    public Incident Incident { get; set; } = null!;

    /// <summary>Summary of the company where the incident occurred.</summary>
    public Company? Company { get; set; }

    /// <summary>The workplace department where the incident occurred (the replacement for the legacy <c>DepartmentName</c> field).</summary>
    public WorkplaceDepartment? Department { get; set; }

    /// <summary>Incident report or photographic evidence (the replacement for the legacy <c>GDocument*</c> fields).</summary>
    public Document? Document { get; set; }

    /// <summary>Employee record of the unit supervisor.</summary>
    public CompanyEmployee? UnitSupervisor { get; set; }

    /// <summary>People affected by the incident (legacy <c>[NotMapped] AffectedPersons</c>).</summary>
    public List<IncidentPerson> AffectedPersons { get; set; } = [];

    /// <summary>People who witnessed the incident (legacy <c>[NotMapped] WitnessPersons</c>).</summary>
    public List<IncidentPerson> WitnessPersons { get; set; } = [];

    /// <summary>People who responded to the incident; legacy had no separate list (<c>IncidentPersonRole.Responder</c>).</summary>
    public List<IncidentPerson> Responders { get; set; } = [];
}
