using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// The "history" section of a risk assessment report: work accidents, no-damage accidents,
/// occupational diseases and near misses that occurred at the workplace previously.
/// <para>
/// NORMALIZATION: legacy had FOUR SEPARATE TABLES that were exact copies of one another
/// (<c>RiskWorkAccidentRecord_T</c>, <c>RiskNoDamageWorkAccidentRecord_T</c>,
/// <c>RiskOccupationHastaliklariRecord_T</c>, <c>RiskNearMissIncidentRecord_T</c>). All four had the
/// same columns: <c>RiskAssessmentReportId</c>, <c>Date</c>, <c>Description</c>,
/// <c>OrganizationId</c>. They were merged into a single table, distinguished by the
/// <see cref="RecordType"/> enum.
/// </para>
/// </summary>
public class RiskAssessmentHistoryRecord : FullAuditedTenantEntity
{
    /// <summary>FK → <see cref="RiskAssessmentReport"/>.</summary>
    public int RiskAssessmentReportId { get; set; }

    /// <summary>Identifies which legacy table the record came from, and what kind of event it is.</summary>
    public RiskHistoryRecordType RecordType { get; set; }

    /// <summary>The date the event occurred.</summary>
    public DateTime Date { get; set; }

    /// <summary>Free-text description of the event.</summary>
    public string Description { get; set; } = string.Empty;
}
