using Ensa.Domain.Common;

namespace Ensa.Domain.Health;

/// <summary>
/// Common base class for the simple SKRS (Health Coding Reference Server) code lists.
/// <para>
/// In the legacy schema the <c>SKRS_MedicationRoute_T</c>, <c>SKRS_MedicationDoseUnit_T</c> and
/// <c>SKRS_MedicationFrequencyUnit_T</c> tables had IDENTICAL layouts (<c>CodeTypeName</c>,
/// <c>Name</c>, <c>Code</c>, <c>Active</c>). Because they are DIFFERENT code lists in SKRS they
/// were not merged into one table; the shared columns were lifted into this abstract base class
/// and the three tables derive from it separately.
/// </para>
/// <para>
/// <b>Note:</b> this is INHERITANCE, not a navigation property — it does not violate the "no
/// navigation properties on entities" rule. On the EF side it is configured as TPC (each derived
/// type maps to its own table).
/// </para>
/// <para>
/// Host-level (tenant-less) reference table: it does NOT implement <c>IMultiTenant</c> and is
/// seeded from SKRS by <c>DbMigrator</c>.
/// </para>
/// </summary>
public abstract class SkrsReferenceEntity : AuditedEntity, IActivatable
{
    /// <summary>Name of the SKRS code list. (Legacy: <c>KodTipiAdi</c>)</summary>
    public string? CodeTypeName { get; set; }

    /// <summary>Display name of the code entry. (Legacy: <c>Adi</c>)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Numeric SKRS code. (Legacy: <c>Kodu</c> <c>int?</c>)</summary>
    public int? Code { get; set; }

    /// <summary>Whether the code is still in use. (Legacy: <c>Aktif</c> <c>bool?</c>)</summary>
    public bool IsActive { get; set; } = true;
}
