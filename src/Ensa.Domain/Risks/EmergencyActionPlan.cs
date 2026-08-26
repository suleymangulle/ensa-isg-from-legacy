using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// The header of an emergency action plan. (Legacy: <c>AcilDurumEylemPlani_T</c>)
/// <para>
/// NORMALIZATION: the legacy table held the plan text in nine separate flat <c>string</c> columns
/// (<c>TableOfContents</c>, <c>Entry</c>, <c>OrganizasyondaYeralanTeamsVeSorumluluklari</c>,
/// <c>Instructions</c>, <c>Wartime</c>, <c>EmergencyTatbikatiApplication</c>,
/// <c>FireControlItemForumu</c>, <c>FirstAid</c>, <c>EmergencyPhones</c>).
/// Those columns were REMOVED entirely from the header; the content now lives in the
/// <see cref="EmergencyPlanSection"/> child table, distinguished by
/// <see cref="EmergencyPlanSectionType"/>. Adding a new section no longer needs a schema change,
/// and sections can be ordered.
/// </para>
/// <para>
/// File conversions: <c>byte[] EvacuationPlani</c> → <see cref="EvacuationPlanDocumentId"/>;
/// <c>byte[] Document</c> + <c>DocumentName</c> + <c>DocumentType</c> → <see cref="DocumentId"/>.
/// </para>
/// </summary>
public class EmergencyActionPlan : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The company the plan belongs to. FK → <c>Company.Id</c>.</summary>
    public int CompanyId { get; set; }

    /// <summary>The date the plan was prepared.</summary>
    public DateTime PreparedDate { get; set; }

    /// <summary>
    /// End of the plan's validity, computed from the hazard class:
    /// very hazardous 2 years, hazardous 4 years, low hazard 6 years.
    /// </summary>
    public DateTime ValidityDate { get; set; }

    // ---- Workplace details as of the report date (legacy kept these on the header too) ----

    /// <summary>Legacy: <c>FirmaAdi</c>.</summary>
    public string? CompanyName { get; set; }

    /// <summary>Legacy: <c>Adres</c>.</summary>
    public string? Address { get; set; }

    /// <summary>Legacy: <c>SicilNo</c>.</summary>
    public string? RegistrationNo { get; set; }

    /// <summary>Legacy: <c>TehlikeSinifi</c> string → enum.</summary>
    public HazardClass HazardClass { get; set; }

    /// <summary>Legacy: <c>Telefon</c>.</summary>
    public string? Phone { get; set; }

    // ---- Signatory and responsible-person fields (free text; carried over verbatim from legacy) ----

    /// <summary>Chief of the emergency response teams. (Legacy: <c>EkiplerSefi</c>)</summary>
    public string? TeamsChief { get; set; }

    /// <summary>Free-text summary of the emergency team. (Legacy: <c>AcilDurumEkibi</c>)</summary>
    public string? EmergencyTeam { get; set; }

    /// <summary>Legacy: <c>CalisanTemsilcisi</c>.</summary>
    public string? WorkerRepresentative { get; set; }

    /// <summary>Legacy: <c>DestekElemani</c>.</summary>
    public string? SupportStaff { get; set; }

    /// <summary>Legacy: <c>IsverenVeyaVekili</c>.</summary>
    public string? EmployerOrDeputy { get; set; }

    /// <summary>Legacy: <c>IsGuvenligiUzmani</c>.</summary>
    public string? OccupationalSafetySpecialist { get; set; }

    /// <summary>Legacy: <c>IsyeriDoktoru</c>.</summary>
    public string? WorkplacePhysician { get; set; }

    /// <summary>Legacy: <c>KorumaPersoneli</c>.</summary>
    public string? ProtectionEmployee { get; set; }

    // ---- Dosyalar ----

    /// <summary>Evacuation plan drawing. FK → <c>Document.Id</c>. (Legacy: <c>byte[] TahliyePlani</c>)</summary>
    public int? EvacuationPlanDocumentId { get; set; }

    /// <summary>The signed or PDF copy of the plan. FK → <c>Document.Id</c>. (Legacy: <c>byte[] Dosya</c>)</summary>
    public int? DocumentId { get; set; }
}
