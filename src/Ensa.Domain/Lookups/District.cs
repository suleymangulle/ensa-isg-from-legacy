using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups;

/// <summary>
/// District reference record.
/// <para>Legacy equivalent: <c>District_T</c>.</para>
/// <para>Host-level (tenant-less) reference table.</para>
/// </summary>
public class District : AuditedEntity
{
    /// <summary>District name.</summary>
    public string DistrictName { get; set; } = string.Empty;

    /// <summary>Owning province. FK — no navigation property.</summary>
    public int CityId { get; set; }

    /// <summary>Official province code (Ministry of Interior MERNIS code). (Legacy: <c>IlKodu</c>)</summary>
    public int? IlCode { get; set; }

    /// <summary>Official district code (MERNIS code). (Legacy: <c>IlceKodu</c>)</summary>
    public int? DistrictCode { get; set; }
}
