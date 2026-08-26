using Ensa.Domain.Common;
using Ensa.Domain.Services;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Domain.Health;

/// <summary>
/// Health surveillance business rules (domain service contract).
/// </summary>
public interface IHealthSurveillanceManager : IDomainService
{
    /// <summary>
    /// Calculates the date of the next periodic examination under Turkish OHS law no. 6331 and
    /// the regulation on the duties, powers, responsibilities and training of workplace
    /// physicians and other health personnel.
    /// </summary>
    DateTime CalculateNextExaminationDate(DateTime examinationDate, HazardClass hazardClass);

    /// <summary>
    /// Returns the periodic examination interval for a hazard class, in years.
    /// </summary>
    int GetExaminationPeriodYear(HazardClass hazardClass);

    /// <summary>
    /// Reports whether the employee is due for a periodic examination.
    /// Returns <c>true</c> when they have no examination at all (a first examination is needed).
    /// </summary>
    /// <param name="companyEmployeeId">The employee record.</param>
    /// <param name="hazardClass">Hazard class of the workplace they work at.</param>
    /// <param name="referenceDate">Comparison date; today when omitted.</param>
    Task<bool> IsExaminationDueAsync(
        int companyEmployeeId,
        HazardClass hazardClass,
        DateTime? referenceDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>Calculates the body mass index (kg/m²).</summary>
    decimal CalculateBmi(int heightCm, decimal weightKg);
}

/// <summary>
/// <inheritdoc cref="IHealthSurveillanceManager"/>
/// <para>
/// In the legacy system these rules were scattered across controllers and helper classes; under
/// DDD they are collected in a domain service and the application services do nothing but call
/// into it.
/// </para>
/// </summary>
public class HealthSurveillanceManager : DomainService, IHealthSurveillanceManager
{
    /// <summary>Periodic examination interval for the low hazard class, in years.</summary>
    public const int AzHazardousPeriodYear = 5;

    /// <summary>Periodic examination interval for the hazardous class, in years.</summary>
    public const int HazardousPeriodYear = 3;

    /// <summary>Periodic examination interval for the very hazardous class, in years.</summary>
    public const int VeryHazardousPeriodYear = 1;

    private readonly IMedicalExaminationFormRepository _examinationFormRepository;
    private readonly IClock _clock;

    public HealthSurveillanceManager(
        IMedicalExaminationFormRepository examinationFormRepository,
        IClock clock)
    {
        _examinationFormRepository = examinationFormRepository;
        _clock = clock;
    }

    /// <inheritdoc />
    public int GetExaminationPeriodYear(HazardClass hazardClass) => hazardClass switch
    {
        HazardClass.LowHazard => AzHazardousPeriodYear,
        HazardClass.Hazardous => HazardousPeriodYear,
        HazardClass.VeryHazardous => VeryHazardousPeriodYear,
        _ => throw new BusinessException(
            "The workplace hazard class must be known before the periodic examination interval can be calculated.",
            "Ensa:Health:HazardClassUnknown")
    };

    /// <inheritdoc />
    public DateTime CalculateNextExaminationDate(DateTime examinationDate, HazardClass hazardClass)
        => examinationDate.Date.AddYears(GetExaminationPeriodYear(hazardClass));

    /// <inheritdoc />
    public async Task<bool> IsExaminationDueAsync(
        int companyEmployeeId,
        HazardClass hazardClass,
        DateTime? referenceDate = null,
        CancellationToken cancellationToken = default)
    {
        var reference = (referenceDate ?? _clock.Now).Date;

        var latestExamination = await _examinationFormRepository.GetLatestExaminationAsync(
            companyEmployeeId,
            reportType: null,
            cancellationToken);

        // With no examination on record, a pre-employment examination is required.
        if (latestExamination is null)
        {
            return true;
        }

        // If the physician wrote an explicit validity date on the report, that wins; otherwise
        // the statutory interval for the hazard class applies.
        var nextDate = latestExamination.ValidityDate?.Date
                           ?? CalculateNextExaminationDate(latestExamination.ExaminationDate, hazardClass);

        return reference >= nextDate;
    }

    /// <inheritdoc />
    public decimal CalculateBmi(int heightCm, decimal weightKg)
    {
        if (heightCm <= 0)
        {
            throw new BusinessException(
                "Height must be greater than zero to calculate a body mass index.",
                "Ensa:Health:InvalidHeight");
        }

        if (weightKg <= 0)
        {
            throw new BusinessException(
                "Weight must be greater than zero to calculate a body mass index.",
                "Ensa:Health:InvalidWeightKg");
        }

        var heightMetres = heightCm / 100m;
        return Math.Round(weightKg / (heightMetres * heightMetres), 2, MidpointRounding.AwayFromZero);
    }
}
