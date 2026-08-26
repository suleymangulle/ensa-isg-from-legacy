using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Lookups;

/// <summary>
/// NACE occupation/activity code reference — a workplace's hazard class is derived from this
/// record.
/// <para>Legacy equivalent: <c>OccupationCode_T</c>.</para>
/// <para>Host-level (tenant-less) reference table.</para>
/// </summary>
public class OccupationCode : AuditedEntity
{
    /// <summary>NACE code. (Legacy: <c>NACE_KODU</c>)</summary>
    public string NaceCode { get; set; } = string.Empty;

    /// <summary>Activity description.</summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Hazard class under Turkish OHS law no. 6331.
    /// (Legacy: <c>TehlikeSinifi</c>, a string such as "AZ TEHLİKELİ" — converted to an enum.)
    /// </summary>
    public HazardClass HazardClass { get; set; } = HazardClass.Unspecified;
}
