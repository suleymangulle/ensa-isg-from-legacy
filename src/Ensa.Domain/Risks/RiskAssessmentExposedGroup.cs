using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// The "groups exposed to the hazard" selected on a report.
/// (Legacy: the ten <c>TMK*</c> boolean columns on <c>RiskAnalizRaporu_T</c>)
/// <para>
/// Legacy had a separate boolean column per group, so adding a group meant a schema change. Now
/// each ticked group is written as its own row.
/// </para>
/// <para>Unique index on (<see cref="RiskAssessmentReportId"/>, <see cref="Group"/>).</para>
/// </summary>
public class RiskAssessmentExposedGroup : CreationAuditedTenantEntity
{
    /// <summary>FK → <see cref="RiskAssessmentReport"/>.</summary>
    public int RiskAssessmentReportId { get; set; }

    /// <summary>The exposed group that was ticked.</summary>
    public ExposedPersonGroup Group { get; set; }
}
