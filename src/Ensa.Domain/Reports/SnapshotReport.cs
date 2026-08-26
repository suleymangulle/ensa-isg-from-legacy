using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Reports;

/// <summary>
/// A periodic snapshot table. It lets heavy aggregation and statistics screens read a
/// pre-computed JSON payload instead of running a live query. <see cref="JsonData"/> is kept as
/// JSON DELIBERATELY: normalizing buys nothing for statistics structures whose schema changes
/// often and keeps evolving.
/// <para>Legacy equivalent: <c>BaselineCompanyTablosu</c>.</para>
/// </summary>
public class SnapshotReport : CreationAuditedTenantEntity
{
    public DateTime ReportDate { get; set; }

    /// <summary>Pre-computed report payload (nvarchar(max)). Deliberately left un-normalized.</summary>
    public string JsonData { get; set; } = string.Empty;

    /// <summary>FK — no navigation property.</summary>
    public int OfficeId { get; set; }

    /// <summary>(Legacy: <c>Tur</c> string)</summary>
    public SnapshotReportType ReportType { get; set; } = SnapshotReportType.Unspecified;
}
