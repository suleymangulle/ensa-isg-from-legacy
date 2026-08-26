using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// A single free-text section of an emergency action plan.
/// <para>
/// NORMALIZATION: this replaces the nine flat string columns on the legacy
/// <c>EmergencyActionPlan_T</c> (Icindekiler, Giris,
/// OrganizasyondaYeralanEkiplerVeSorumluluklari, Talimatlar, Savas,
/// AcilDurumTatbikatiUygulamasi, YanginKontrolForumu, IlkYardim, AcilDurumTelefonlari).
/// Sections are distinguished by <see cref="SectionType"/>.
/// </para>
/// <para>Unique index on (<see cref="EmergencyActionPlanId"/>, <see cref="SectionType"/>).</para>
/// </summary>
public class EmergencyPlanSection : FullAuditedTenantEntity
{
    /// <summary>FK → <see cref="EmergencyActionPlan"/>.</summary>
    public int EmergencyActionPlanId { get; set; }

    /// <summary>Type of the section; it identifies which legacy column the content came from.</summary>
    public EmergencyPlanSectionType SectionType { get; set; }

    /// <summary>Section text; it may be HTML or rich text.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Print order of the section within the plan.</summary>
    public int OrderNo { get; set; }
}
