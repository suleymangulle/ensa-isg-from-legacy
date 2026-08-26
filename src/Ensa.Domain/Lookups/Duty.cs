using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Duty/title reference record (e.g. "Occupational Safety Specialist", "Workplace Physician").
/// <para>Legacy equivalent: <c>Duty_T</c>.</para>
/// <para>
/// Host-level (tenant-less) reference table. The legacy table had no audit columns; for
/// consistency with the other host reference tables it derives from <see cref="AuditedEntity"/>
/// (no data is lost, only extra traceability is gained).
/// </para>
/// </summary>
public class Duty : AuditedEntity, IActivatable
{
    /// <summary>Unique duty code.</summary>
    public string DutyCode { get; set; } = string.Empty;

    /// <summary>Duty name.</summary>
    public string DutyName { get; set; } = string.Empty;

    /// <summary>Short label/abbreviation (e.g. the abbreviation used in report headings).</summary>
    public string? DutyLabel { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
