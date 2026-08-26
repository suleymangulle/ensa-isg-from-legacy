using Ensa.Domain.Common;
using Ensa.Domain.Services;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Domain.Trainings;

/// <summary>
/// Domain service that computes training refresher intervals and mandatory durations according to
/// OHS legislation.
/// <para>
/// Under the OHS training regulation issued below Law No. 6331 the refresher interval is:
/// low hazard = 3 years, hazardous = 2 years, very hazardous = 1 year.
/// The mandatory training duration is: low hazard = 8 hours (480 min), hazardous = 12 hours
/// (720 min), very hazardous = 16 hours (960 min).
/// </para>
/// </summary>
public interface ITrainingPlanningManager : IDomainService
{
    /// <summary>Computes the next (refresher) training date from the given last training date.</summary>
    DateTime CalculateNextTrainingDate(DateTime latestTrainingDate, HazardClass hazardClass);

    /// <summary>Returns the mandatory training duration in minutes for a hazard class.</summary>
    int GetMandatoryDurationMinutes(HazardClass hazardClass);

    /// <summary>
    /// Whether an employee's given training is still valid, that is, its refresher interval has not
    /// elapsed. Returns <c>false</c> when the employee has no distance-learning progress record, or
    /// has not completed the final test.
    /// </summary>
    Task<bool> IsTrainingValidAsync(int companyEmployeeId, int trainingId, HazardClass hazardClass, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that the company has no other active training plan for the year. Throws
    /// <see cref="BusinessException"/> when it does and the plan is not
    /// <paramref name="exceptPlanId"/>.
    /// <para>
    /// This is an invariant of the plan, not a screen rule, so it lives here rather than in the
    /// application service — exactly as <c>IWorkPlanManager.ValidateSingleActivePlanAsync</c>
    /// does for work plans. Enforcing it only in the service would let any other caller create a
    /// second active plan.
    /// </para>
    /// </summary>
    Task ValidateSingleActivePlanAsync(
        int companyId,
        int year,
        int? exceptPlanId = null,
        CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="ITrainingPlanningManager"/>
public class TrainingPlanningManager : DomainService, ITrainingPlanningManager
{
    private readonly IEmployeeTrainingProgressRepository _progressRepository;
    private readonly IClock _clock;
    private readonly ITrainingPlanRepository _planRepository;

    public TrainingPlanningManager(
        IEmployeeTrainingProgressRepository progressRepository,
        IClock clock,
        ITrainingPlanRepository planRepository)
    {
        _progressRepository = progressRepository;
        _clock = clock;
        _planRepository = planRepository;
    }

    /// <inheritdoc />
    public async Task ValidateSingleActivePlanAsync(
        int companyId,
        int year,
        int? exceptPlanId = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _planRepository.GetActivePlanAsync(companyId, year, cancellationToken);

        if (existing is not null && existing.Id != exceptPlanId)
        {
            throw new BusinessException(
                    "This workplace already has an active training plan for the given year. "
                    + "Deactivate the current plan before creating a new one.",
                    "Ensa:TrainingPlan:SingleActivePlanViolation")
                .WithData("CompanyId", companyId)
                .WithData("Year", year);
        }
    }

    public DateTime CalculateNextTrainingDate(DateTime latestTrainingDate, HazardClass hazardClass)
    {
        var refresherYear = RefresherRangeYear(hazardClass);
        return latestTrainingDate.AddYears(refresherYear);
    }

    public int GetMandatoryDurationMinutes(HazardClass hazardClass) => hazardClass switch
    {
        HazardClass.LowHazard => 8 * 60,
        HazardClass.Hazardous => 12 * 60,
        HazardClass.VeryHazardous => 16 * 60,
        _ => throw HazardClassRequiredError()
    };

    public async Task<bool> IsTrainingValidAsync(
        int companyEmployeeId,
        int trainingId,
        HazardClass hazardClass,
        CancellationToken cancellationToken = default)
    {
        var progress = await _progressRepository.FindAsync(
            companyEmployeeId, trainingId, trainingTopicId: null, cancellationToken);

        if (progress is null || !progress.LatestTestCompleted)
            return false;

        var completionDate = progress.LastModificationTime ?? progress.CreationTime;
        var nextTrainingDate = CalculateNextTrainingDate(completionDate, hazardClass);

        return nextTrainingDate > _clock.Now;
    }

    private static int RefresherRangeYear(HazardClass hazardClass) => hazardClass switch
    {
        HazardClass.LowHazard => 3,
        HazardClass.Hazardous => 2,
        HazardClass.VeryHazardous => 1,
        _ => throw HazardClassRequiredError()
    };

    private static BusinessException HazardClassRequiredError() => new(
        "The training refresher rule cannot be applied without a hazard class.",
        "Ensa:Training:HazardClassRequired");
}
