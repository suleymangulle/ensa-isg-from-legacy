using Ensa.Domain.Common;
using Ensa.Domain.Trainings;
using Ensa.Domain.Trainings.Navigations;
using Ensa.Domain.Companies;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Trainings;

/// <summary>
/// EF Core implementation of <see cref="IEmployeeTrainingProgressRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class EmployeeTrainingProgressRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<EmployeeTrainingProgress>(context, dataFilter), IEmployeeTrainingProgressRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// When <paramref name="trainingTopicId"/> is <c>null</c> the <b>training-wide</b> (non topic-based)
    /// progress record is looked up; this is the behaviour <c>TrainingPlanningManager</c> expects.
    /// </remarks>
    public Task<EmployeeTrainingProgress?> FindAsync(
        int companyEmployeeId,
        int trainingId,
        int? trainingTopicId,
        CancellationToken cancellationToken = default)
        => GetQueryable()
            .FirstOrDefaultAsync(
                i => i.CompanyEmployeeId == companyEmployeeId
                     && i.TrainingId == trainingId
                     && i.TrainingTopicId == trainingTopicId,
                cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="EmployeeTrainingProgressNavigation.RemainingDurationSeconds"/> is derived from the
    /// <see cref="TrainingDuration"/> record matching the hazard class of the employee's company.
    /// When there is no duration record the remaining time is taken as 0. The total query count is
    /// constant (at most 5).
    /// </remarks>
    public async Task<EmployeeTrainingProgressNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var progress = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (progress is null)
        {
            return null;
        }

        var navigation = new EmployeeTrainingProgressNavigation { Progress = progress };

        navigation.Employee = await Context.Set<CompanyEmployee>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == progress.CompanyEmployeeId, cancellationToken);

        navigation.Training = await Context.Set<Training>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == progress.TrainingId, cancellationToken);

        var hazardClass = HazardClass.Unspecified;
        if (navigation.Employee is { } employee)
        {
            hazardClass = await Context.Set<Company>()
                .AsNoTracking()
                .Where(f => f.Id == employee.CompanyId)
                .Select(f => f.HazardClass)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var mandatoryMinutes = hazardClass == HazardClass.Unspecified
            ? null
            : await Context.Set<TrainingDuration>()
                .AsNoTracking()
                .Where(s => s.TrainingId == progress.TrainingId && s.HazardClass == hazardClass)
                .Select(s => (int?)s.DurationMinutes)
                .FirstOrDefaultAsync(cancellationToken);

        var totalSeconds = (mandatoryMinutes ?? 0) * 60;
        navigation.RemainingDurationSeconds = Math.Max(0, totalSeconds - progress.ElapsedDurationSeconds);

        return navigation;
    }

    /// <inheritdoc />
    public Task<List<EmployeeTrainingProgress>> GetEmployeeProgressAsync(
        int companyEmployeeId,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .Where(i => i.CompanyEmployeeId == companyEmployeeId)
            .OrderBy(i => i.TrainingId)
            .ThenBy(i => i.TrainingTopicId)
            .ToListAsync(cancellationToken);
}
