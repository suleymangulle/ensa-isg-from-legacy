using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// A person involved in an incident: affected, witness or responder.
/// (Legacy: <c>OlayKisi_T</c>, which was declared inside the <c>Incident_T.cs</c> file.)
/// <para>
/// Conversions: <c>PersonType byte</c> → the <see cref="IncidentPersonRole"/> enum;
/// <c>EmployeeId</c> → <see cref="CompanyEmployeeId"/>. Legacy had no <c>OrganizationId</c> column
/// and derived the tenant from the parent record. Here the tenant field comes from the base class
/// and is filled with the incident's tenant, so the global query filter behaves consistently.
/// </para>
/// </summary>
public class IncidentPerson : FullAuditedTenantEntity
{
    /// <summary>FK → <see cref="Incident"/>.</summary>
    public int IncidentId { get; set; }

    /// <summary>The person's role in the incident. (Legacy: <c>KisiTur byte</c>)</summary>
    public IncidentPersonRole PersonType { get; set; }

    /// <summary>The matching company employee. FK → <c>CompanyEmployee.Id</c>. (Legacy: <c>PersonelId</c>)</summary>
    public int? CompanyEmployeeId { get; set; }

    /// <summary>First name; typed in when there is no employee record.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Surname.</summary>
    public string LastName { get; set; } = string.Empty;
}
