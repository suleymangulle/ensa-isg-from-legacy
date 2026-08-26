using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Reports;

/// <summary>
/// A single data row within an <see cref="ActivityReport"/>, produced by the report engine.
/// <para>Legacy equivalent: <c>FaaliyetReportLine_T</c>.</para>
/// </summary>
public class ActivityReportLine : CreationAuditedTenantEntity
{
    /// <summary>(Legacy: <c>RaporId</c>) FK — no navigation property.</summary>
    public int ActivityReportId { get; set; }

    /// <summary>(Legacy: <c>SatirTuru</c> string)</summary>
    public ActivityReportLineType LineType { get; set; }

    public string? Text { get; set; }

    public string? Value1 { get; set; }

    public string? Value2 { get; set; }

    public string? Value3 { get; set; }

    /// <summary>Display order within the report. Not present in legacy; added to give a stable order.</summary>
    public int OrderNo { get; set; }
}
