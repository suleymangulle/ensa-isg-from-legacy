using Ensa.Domain.Repositories;
using Ensa.Domain.Risks.Navigations;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// Queries specific to risk assessment reports. The implementation lives under
/// <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IRiskAssessmentReportRepository : IRepository<RiskAssessmentReport>
{
    /// <summary>
    /// Loads the report with all of its child records: identified hazards, control measures, exposed
    /// groups, existing protections, improvement actions, protected groups, participants and history
    /// records.
    /// </summary>
    Task<RiskAssessmentReportNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the company's report that is in force on the given date — approved and not yet expired.
    /// If several qualify, the one with the latest <c>PerformedDate</c> is returned.
    /// </summary>
    /// <param name="companyId">Company id.</param>
    /// <param name="referenceDate">The date the validity check is made against; today by default.</param>
    Task<RiskAssessmentReport?> GetActiveReportAsync(
        int companyId,
        DateTime? referenceDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists reports that had expired as of <paramref name="reference"/>, or that expire within
    /// <paramref name="remainingDayThreshold"/> days.
    /// </summary>
    /// <param name="reference">Reference date.</param>
    /// <param name="remainingDayThreshold">0 → expired reports only; &gt;0 → upcoming expiries too.</param>
    /// <param name="companyId">Optional; narrows the result to a single company.</param>
    Task<List<RiskAssessmentReport>> GetDurationExpiredAsync(
        DateTime reference,
        int remainingDayThreshold = 0,
        int? companyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the report's hazard lines that either have no residual score yet, or whose score is
    /// still above the given level — the action follow-up list.
    /// </summary>
    Task<List<IdentifiedHazard>> GetOpenHighRiskHazardsAsync(
        int riskAssessmentReportId,
        RiskLevel minimumLevel = RiskLevel.High,
        CancellationToken cancellationToken = default);
}
