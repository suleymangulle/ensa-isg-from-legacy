using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Reports;

/// <summary>
/// Summary report of one employee's (OHS specialist or workplace physician) service hours and
/// assignments over a period. The hazard class breakdown lives in
/// <see cref="OhsReportHazardClassBreakdown"/>.
/// <para>Legacy equivalent: <c>ISGReport_T</c>.</para>
/// <para>
/// NORMALIZATION: the legacy <c>AzHazardousCount</c>/<c>HazardousCount</c>/<c>VeryHazardousCount</c>
/// column triple was REMOVED from the header and normalized into the
/// <see cref="OhsReportHazardClassBreakdown"/> child table.
/// </para>
/// </summary>
public class OhsReport : CreationAuditedTenantEntity
{
    /// <summary>FK — no navigation property.</summary>
    public int OfficeId { get; set; }

    /// <summary>The archive detail record the report originated from. FK — no navigation property.</summary>
    public int ModuleArchiveDetailId { get; set; }

    public string NationalId { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>(Legacy: <c>PersonelTuru</c> string)</summary>
    public StaffRole StaffRole { get; set; } = StaffRole.Unspecified;

    /// <summary>(Legacy: <c>GorevTuru</c> string "İçe Grv."/"Dışa Grv.")</summary>
    public AssignmentType DutyType { get; set; } = AssignmentType.Unspecified;

    public int TotalMonthlyFazlaOvertimeDuration { get; set; }

    public int TotalMinutes { get; set; }

    public int UsedMonthlyMinutes { get; set; }
}
