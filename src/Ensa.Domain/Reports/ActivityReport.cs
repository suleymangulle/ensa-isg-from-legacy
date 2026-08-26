using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Reports;

/// <summary>
/// The header of a periodic activity report produced for a company. Its rows live in
/// <see cref="ActivityReportLine"/>.
/// <para>
/// Legacy equivalent: <c>FaliyetReport_T</c> (the typo was fixed: "Faliyet" → "Faaliyet").
/// </para>
/// </summary>
public class ActivityReport : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The company the report was produced for. FK — no navigation property.</summary>
    public int CompanyId { get; set; }

    /// <summary>(Legacy: <c>RaporTuru</c> string)</summary>
    public ActivityReportType ReportType { get; set; } = ActivityReportType.Unspecified;

    public string ReportName { get; set; } = string.Empty;

    public DateTime ReportStart { get; set; }

    public DateTime ReportEnd { get; set; }
}
