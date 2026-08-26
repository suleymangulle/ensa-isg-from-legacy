using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Certificate type reference record (e.g. "Class A Occupational Safety Specialist Certificate").
/// <para>Legacy equivalent: <c>CertificateList_T</c>.</para>
/// <para>Host-level (tenant-less) reference table.</para>
/// </summary>
public class Certificate : AuditedEntity
{
    /// <summary>Certificate name.</summary>
    public string CertificateName { get; set; } = string.Empty;

    /// <summary>Certificate code.</summary>
    public string CertificateCode { get; set; } = string.Empty;
}
