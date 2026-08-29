using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// An incident record: work accident, near miss or occupational disease. (Legacy: <c>Olay_T</c>)
/// <para>
/// Conversions: <c>IncidentType byte</c> → the <see cref="IncidentType"/> enum;
/// <c>AccidentType byte?</c> → the <see cref="AccidentType"/> enum; <c>GDocumentId</c> →
/// <see cref="DocumentId"/>.
/// Every legacy <c>[NotMapped]</c> field (<c>AffectedPersons</c>, <c>WitnessPersons</c>,
/// <c>GDocument</c>, <c>GDocumentName</c>, <c>GDocumentType</c>, <c>DepartmentName</c>) was removed
/// from the entity; their counterparts live in <c>Navigations\IncidentNavigation</c>.
/// </para>
/// </summary>
public class Incident : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The company where the incident occurred. FK → <c>Company.Id</c>.</summary>
    public int CompanyId { get; set; }

    /// <summary>The workplace department where the incident occurred. FK → <c>WorkplaceDepartment.Id</c>.</summary>
    public int DepartmentId { get; set; }

    /// <summary>Incident type. (Legacy: <c>OlayTuru byte</c>)</summary>
    public IncidentType IncidentType { get; set; }

    /// <summary>Accident type; meaningful only for work accidents and no-damage incidents. (Legacy: <c>KazaTuru byte?</c>)</summary>
    public AccidentType AccidentType { get; set; } = AccidentType.Unspecified;

    /// <summary>
    /// How severe the accident was. (Legacy: <c>Olay_T.KazaTuru</c>, which despite its name is a
    /// severity scale, not a mechanism.)
    /// </summary>
    public AccidentSeverity AccidentSeverity { get; set; } = AccidentSeverity.Unspecified;

    /// <summary>Date and time the incident occurred.</summary>
    public DateTime IncidentDate { get; set; }

    /// <summary>Description of the incident. (Legacy: <c>Aciklama</c>)</summary>
    public string? Description { get; set; }

    /// <summary>Statement given by the injured person or a witness. (Legacy: <c>Ifade</c>)</summary>
    public string? Expression { get; set; }

    /// <summary>Incident report or photographic evidence. FK → <c>Document.Id</c>. (Legacy: <c>GDosyaId</c>)</summary>
    public int? DocumentId { get; set; }

    /// <summary>Supervisor of the unit where the incident occurred. FK → <c>CompanyEmployee.Id</c>. (Legacy: <c>BirimAmirId</c>)</summary>
    public int? UnitSupervisorId { get; set; }

    /// <summary>Full name of the unit supervisor, as recorded at the time. (Legacy: <c>AmirAdSoyad</c>)</summary>
    public string? SupervisorFullName { get; set; }

    // ---- Lost working days (added to meet legal requirements) ----

    /// <summary>
    /// Working days lost as a result of the accident.
    /// <para>
    /// NOT PRESENT IN LEGACY — added to meet Law No. 6331 and the SSI work accident reporting
    /// rules. It is an input to the accident frequency and severity rates in the annual review
    /// report.
    /// </para>
    /// </summary>
    public int? LostWorkDays { get; set; }

    /// <summary>
    /// The date the injured person returned to work.
    /// <para>NOT PRESENT IN LEGACY — added to meet the legal requirement to track incapacity
    /// periods.</para>
    /// </summary>
    public DateTime? ReturnToWorkDate { get; set; }

    /// <summary>
    /// The date the incident was reported to the SSI.
    /// <para>
    /// NOT PRESENT IN LEGACY — under article 13 of Law No. 5510 a work accident must be reported to
    /// the SSI within three working days. <c>IIncidentManager</c> checks for late reporting through
    /// this field.
    /// </para>
    /// </summary>
    public DateTime? SsiNotificationDate { get; set; }
}
