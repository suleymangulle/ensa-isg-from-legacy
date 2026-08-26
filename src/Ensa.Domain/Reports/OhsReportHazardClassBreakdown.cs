using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Reports;

/// <summary>
/// NEW ENTITY. The number of companies covered by an <see cref="OhsReport"/>, broken down by
/// hazard class; the normalized form of the legacy
/// <c>AzHazardousCount</c>/<c>HazardousCount</c>/<c>VeryHazardousCount</c> column triple (see the
/// <see cref="OhsReport"/> XML doc).
/// <para>Unique on (<see cref="OhsReportId"/>, <see cref="HazardClass"/>).</para>
/// </summary>
public class OhsReportHazardClassBreakdown : CreationAuditedTenantEntity
{
    /// <summary>FK — no navigation property.</summary>
    public int OhsReportId { get; set; }

    public HazardClass HazardClass { get; set; }

    public int CompanyCount { get; set; }
}
