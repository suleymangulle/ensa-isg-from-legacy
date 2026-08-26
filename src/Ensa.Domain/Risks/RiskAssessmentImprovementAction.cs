using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// The "improvement actions" selected on a report.
/// (Legacy: the seven <c>IO*</c> boolean columns on <c>RiskAnalizRaporu_T</c>)
/// <para>Unique index on (<see cref="RiskAssessmentReportId"/>, <see cref="Recommendation"/>).</para>
/// </summary>
public class RiskAssessmentImprovementAction : CreationAuditedTenantEntity
{
    /// <summary>FK → <see cref="RiskAssessmentReport"/>.</summary>
    public int RiskAssessmentReportId { get; set; }

    /// <summary>The improvement action that was ticked.</summary>
    public ImprovementAction Recommendation { get; set; }
}
