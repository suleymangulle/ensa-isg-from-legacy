using Ensa.Domain.Services;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Domain.Risks;

/// <summary>
/// Risk assessment business rules: computing the risk score, determining the risk level and working
/// out the report's expiry date.
/// <para>
/// Legacy duplicated these calculations both in AngularJS
/// (<c>RiskAssessmentReportController.js</c>) and, scattered about, on the server. This class is
/// the single source of truth.
/// </para>
/// </summary>
public interface IRiskAssessmentManager : IDomainService
{
    /// <summary>
    /// Computes the hazard line's risk score before controls — and, when the values are present, after
    /// controls — using the method of the report it belongs to, and writes them to
    /// <see cref="IdentifiedHazard.RiskScore"/> and
    /// <see cref="IdentifiedHazard.ResidualRiskScore"/>.
    /// </summary>
    /// <returns>The computed risk score before controls.</returns>
    Task<decimal> CalculateAsync(IdentifiedHazard hazard, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes a score where the method is already known, without touching the repository.
    /// </summary>
    /// <param name="likelihood">Likelihood value.</param>
    /// <param name="severity">Severity — the degree of harm.</param>
    /// <param name="frequency">Frequency; used by Fine-Kinney only.</param>
    /// <param name="method">The risk assessment methodology.</param>
    decimal Calculate(decimal likelihood, decimal severity, decimal? frequency, RiskAssessmentMethod method);

    /// <summary>Returns the risk level the score maps to, against the thresholds of the given method.</summary>
    RiskLevel DetermineLevel(decimal score, RiskAssessmentMethod method);

    /// <summary>
    /// Computes the date the risk assessment must be renewed.
    /// Law No. 6331 / Risk Assessment Regulation, article 12: very hazardous 2 years, hazardous
    /// 4 years, low hazard 6 years.
    /// </summary>
    DateTime CalculateValidUntilDate(DateTime performed, HazardClass hazardClass);

    /// <summary>Returns whether the report is still valid on the given date.</summary>
    bool IsValid(RiskAssessmentReport report, DateTime reference);
}

/// <inheritdoc cref="IRiskAssessmentManager"/>
public class RiskAssessmentManager : DomainService, IRiskAssessmentManager
{
    private readonly IRiskAssessmentReportRepository _riskAssessmentReportRepository;

    public RiskAssessmentManager(IRiskAssessmentReportRepository riskAssessmentReportRepository)
        => _riskAssessmentReportRepository = riskAssessmentReportRepository;

    /// <inheritdoc/>
    public async Task<decimal> CalculateAsync(IdentifiedHazard hazard, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hazard);

        var report = await _riskAssessmentReportRepository.FindAsync(hazard.RiskAssessmentReportId, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(RiskAssessmentReport), hazard.RiskAssessmentReportId);

        var method = report.ReportMethod;

        hazard.RiskScore = Calculate(hazard.Likelihood, hazard.Severity, hazard.Frequency, method);
        hazard.ResidualRiskScore = CalculateResidual(hazard, method);

        return hazard.RiskScore;
    }

    /// <inheritdoc/>
    public decimal Calculate(decimal likelihood, decimal severity, decimal? frequency, RiskAssessmentMethod method)
    {
        if (likelihood <= 0 || severity <= 0)
        {
            throw new EnsaValidationException(
                nameof(IdentifiedHazard.Likelihood),
                "Likelihood and severity must both be greater than zero.");
        }

        return method switch
        {
            // L-Matrix (3x3 and 5x5): risk = likelihood x severity
            RiskAssessmentMethod.LMatrixThreeByThree or RiskAssessmentMethod.LMatrixFiveByFive
                => likelihood * severity,

            // Fine-Kinney: risk = likelihood x frequency x severity
            RiskAssessmentMethod.FineKinney
                => likelihood
                   * (frequency is > 0
                       ? frequency.Value
                       : throw new EnsaValidationException(
                           nameof(IdentifiedHazard.Frequency),
                           "The Fine-Kinney method requires a frequency greater than zero."))
                   * severity,

            // FMEA / checklist: these produce no numeric score, so nothing is scored.
            RiskAssessmentMethod.Fmea or RiskAssessmentMethod.Checklist => 0m,

            _ => throw new BusinessException(
                "The risk score could not be computed: the report method is not set.",
                "Ensa:Risk:MethodUnspecified")
        };
    }

    /// <inheritdoc/>
    public RiskLevel DetermineLevel(decimal score, RiskAssessmentMethod method) => method switch
    {
        RiskAssessmentMethod.FineKinney => score switch
        {
            > 400m => RiskLevel.Intolerable,   // Intolerable — the activity must be stopped
            > 200m => RiskLevel.High,          // High — improve in the short term
            > 70m => RiskLevel.Medium,         // Substantial — improve to a plan
            > 20m => RiskLevel.Low,            // Possible — keep under review
            _ => RiskLevel.Negligible          // Negligible — acceptable
        },

        RiskAssessmentMethod.LMatrixFiveByFive => score switch
        {
            >= 25m => RiskLevel.Intolerable,
            >= 15m => RiskLevel.High,
            >= 8m => RiskLevel.Medium,
            >= 3m => RiskLevel.Low,
            _ => RiskLevel.Negligible
        },

        RiskAssessmentMethod.LMatrixThreeByThree => score switch
        {
            >= 9m => RiskLevel.Intolerable,
            >= 6m => RiskLevel.High,
            >= 3m => RiskLevel.Medium,
            >= 2m => RiskLevel.Low,
            _ => RiskLevel.Negligible
        },

        // Methods that produce no numeric score have no level.
        _ => RiskLevel.Unspecified
    };

    /// <inheritdoc/>
    public DateTime CalculateValidUntilDate(DateTime performed, HazardClass hazardClass)
    {
        var year = hazardClass switch
        {
            HazardClass.VeryHazardous => 2,
            HazardClass.Hazardous => 4,
            HazardClass.LowHazard => 6,
            _ => throw new EnsaValidationException(
                nameof(RiskAssessmentReport.HazardClass),
                "The workplace hazard class is required to compute the expiry date.")
        };

        return performed.Date.AddYears(year);
    }

    /// <inheritdoc/>
    public bool IsValid(RiskAssessmentReport report, DateTime reference)
    {
        ArgumentNullException.ThrowIfNull(report);
        return !report.IsDeleted && report.ValidityDate.Date >= reference.Date;
    }

    /// <summary>
    /// Computes the residual score once every after-controls value has been entered; returns
    /// <c>null</c> otherwise.
    /// </summary>
    private decimal? CalculateResidual(IdentifiedHazard hazard, RiskAssessmentMethod method)
    {
        if (hazard.ResidualLikelihood is not { } likelihood || hazard.ResidualSeverity is not { } severity)
        {
            return null;
        }

        if (method == RiskAssessmentMethod.FineKinney && hazard.ResidualFrequency is not > 0)
        {
            return null;
        }

        return Calculate(likelihood, severity, hazard.ResidualFrequency, method);
    }
}
