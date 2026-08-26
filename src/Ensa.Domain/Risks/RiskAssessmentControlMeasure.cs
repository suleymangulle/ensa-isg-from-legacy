using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// The "existing control measures" selected on a report.
/// (Legacy: the seven <c>MKO*</c> boolean columns on <c>RiskAnalizRaporu_T</c>)
/// <para>Unique index on (<see cref="RiskAssessmentReportId"/>, <see cref="Measure"/>).</para>
/// </summary>
public class RiskAssessmentControlMeasure : CreationAuditedTenantEntity
{
    /// <summary>FK → <see cref="RiskAssessmentReport"/>.</summary>
    public int RiskAssessmentReportId { get; set; }

    /// <summary>The existing control measure that was ticked.</summary>
    public ExistingControlMeasure Measure { get; set; }
}
