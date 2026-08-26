using Ensa.Domain.Common;

namespace Ensa.Domain.Risks;

/// <summary>
/// An entry in the hazard library. Risk assessment reports pick it as a ready-made
/// hazard / risk / control-measure triple. (Legacy: <c>Tehlike_T</c>)
/// <para>
/// TENANCY DECISION: the legacy table had NO <c>OrganizationId</c> column — the library was shared
/// by every organization, and system rows were flagged with <c>DefaultHazard</c>.
/// <c>ARCHITECTURE.md §5</c> counts this table among the host reference tables. To let
/// organizations add their own hazards, the <see cref="AuditedTenantEntity"/> base was chosen
/// instead: <c>TenantId = null</c> → the SHARED (host) library, <c>TenantId != null</c> → a hazard
/// owned by one organization. Legacy data is migrated with <c>TenantId = null</c> throughout, so
/// behaviour stays identical to legacy while organization-specific rows can be added later without
/// a schema change.
/// </para>
/// </summary>
public class Hazard : AuditedTenantEntity, IActivatable
{
    /// <summary>FK → <see cref="HazardCategory"/>.</summary>
    public int HazardCategoryId { get; set; }

    /// <summary>Description of the hazard. (Legacy: the <c>Tehlike</c> column)</summary>
    public string HazardTag { get; set; } = string.Empty;

    /// <summary>The risk the hazard gives rise to. (Legacy: <c>Risk</c>)</summary>
    public string? RiskTag { get; set; }

    /// <summary>The recommended control measure. (Legacy: <c>Onlem</c>)</summary>
    public string? Measure { get; set; }

    /// <summary>Default likelihood value; it is copied into the report.</summary>
    public decimal Likelihood { get; set; }

    /// <summary>Default severity value.</summary>
    public decimal Severity { get; set; }

    /// <summary>Default frequency value, used by Fine-Kinney.</summary>
    public decimal Frequency { get; set; }

    /// <summary>
    /// Whether this is a system-defined default hazard.
    /// (Legacy: <c>DefaultTehlike</c>) Users can neither delete nor edit these records.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>Legacy: <c>Aktif</c>.</summary>
    public bool IsActive { get; set; } = true;
}
