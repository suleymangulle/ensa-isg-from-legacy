using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// The "worker groups requiring a special policy" present at the workplace.
/// (Legacy: the <c>RiskAnalizRaporu_T.KadinCalisan/YasliCalisan/CocukCalisan/EngelliCalisan</c> boolean columns)
/// <para>
/// Legacy supported only four groups and could not record a headcount. This model supports every
/// group named in Law No. 6331 — pregnant and breastfeeding workers and young workers included —
/// and can optionally record the number of workers in each.
/// </para>
/// <para>Unique index on (<see cref="RiskAssessmentReportId"/>, <see cref="Group"/>).</para>
/// </summary>
public class RiskAssessmentProtectedGroup : CreationAuditedTenantEntity
{
    /// <summary>FK → <see cref="RiskAssessmentReport"/>.</summary>
    public int RiskAssessmentReportId { get; set; }

    /// <summary>The special group ticked as present at the workplace.</summary>
    public VulnerableWorkerGroup Group { get; set; }

    /// <summary>
    /// Number of workers in the group. Legacy had no equivalent — it recorded only presence or
    /// absence. It is optional and stays <c>null</c> when not entered.
    /// </summary>
    public int? Number { get; set; }
}
