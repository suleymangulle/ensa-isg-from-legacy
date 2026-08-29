using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Trainings;
using Ensa.Application.Contracts.Trainings.Dtos;
using Ensa.Application.Contracts.Trainings.Dtos.Navigations;
using Ensa.Domain.Companies;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Ensa.Domain.Trainings;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Trainings;

/// <summary>
/// Remote-learning progress of an employee across the trainings assigned to them.
/// <para>
/// Elapsed time is monotonic: a value lower than the one already stored is ignored. Client
/// timers replay on refresh and arrive out of order, and a naive assignment would let a
/// learner shrink their recorded time — which for statutory training is a compliance
/// falsification, not a display glitch.
/// </para>
/// </summary>
public class EmployeeTrainingProgressAppService(
    IServiceProvider serviceProvider,
    IEmployeeTrainingProgressRepository progressRepository,
    ITrainingPlanningManager trainingPlanningManager,
    IReadOnlyRepository<Training> trainingRepository,
    IReadOnlyRepository<CompanyEmployee> employeeRepository,
    IReadOnlyRepository<Company> companyRepository)
    : EnsaAppService(serviceProvider), IEmployeeTrainingProgressAppService
{
    /// <inheritdoc />
    public async Task<EmployeeTrainingProgressDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Training.Default);

        var progress = await progressRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(EmployeeTrainingProgress), id);

        return ObjectMapper.Map<EmployeeTrainingProgress, EmployeeTrainingProgressDto>(progress);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<EmployeeTrainingProgressDto>> GetForEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Training.Default);

        var records = await progressRepository.GetEmployeeProgressAsync(employeeId, cancellationToken);

        return new ListResultDto<EmployeeTrainingProgressDto>(
            ObjectMapper.Map<List<EmployeeTrainingProgress>, List<EmployeeTrainingProgressDto>>(records));
    }

    /// <inheritdoc />
    public async Task<EmployeeTrainingProgressNavigationDto> GetNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Training.Default);

        // The repository returns the progress record, the employee, the training and the
        // remaining seconds in a single projection.
        var navigation = await progressRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(EmployeeTrainingProgress), id);

        return new EmployeeTrainingProgressNavigationDto
        {
            Progress = ObjectMapper.Map<EmployeeTrainingProgress, EmployeeTrainingProgressDto>(navigation.Progress),
            Employee = navigation.Employee is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Employee.Id,
                    DisplayName = $"{navigation.Employee.Name} {navigation.Employee.LastName}".Trim(),
                    IsActive = navigation.Employee.IsActive
                },
            Training = navigation.Training is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Training.Id,
                    DisplayName = navigation.Training.TrainingName,
                    Code = navigation.Training.TrainingCode,
                    IsActive = navigation.Training.IsActive
                },
            RemainingDurationSeconds = Math.Max(0, navigation.RemainingDurationSeconds)
        };
    }

    /// <inheritdoc />
    public async Task<EmployeeTrainingProgressDto> StartAsync(
        StartTrainingProgressDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Training.Create);

        _ = await employeeRepository.FindAsync(input.CompanyEmployeeId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(CompanyEmployee), input.CompanyEmployeeId);

        var training = await trainingRepository.FindAsync(input.TrainingId, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Training), input.TrainingId);

        if (!training.IsActive)
        {
            throw new BusinessException(
                "An inactive training cannot be started.",
                "Ensa:Training:InactiveCannotBeStarted")
                .WithData("TrainingId", input.TrainingId);
        }

        // Starting is idempotent: a learner who reopens the course keeps the progress they
        // already have instead of silently restarting from zero.
        var existing = await progressRepository.FindAsync(
            input.CompanyEmployeeId,
            input.TrainingId,
            input.TrainingTopicId,
            cancellationToken);

        if (existing is not null)
        {
            return ObjectMapper.Map<EmployeeTrainingProgress, EmployeeTrainingProgressDto>(existing);
        }

        var progress = ObjectMapper.Map<StartTrainingProgressDto, EmployeeTrainingProgress>(input);
        progress.IsActive = true;

        progress = await progressRepository.InsertAsync(progress, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Remote training started. ProgressId={ProgressId}, EmployeeId={EmployeeId}, TrainingId={TrainingId}",
            progress.Id, progress.CompanyEmployeeId, progress.TrainingId);

        return ObjectMapper.Map<EmployeeTrainingProgress, EmployeeTrainingProgressDto>(progress);
    }

    /// <inheritdoc />
    public async Task<EmployeeTrainingProgressDto> SaveTopicProgressAsync(
        int id,
        SaveTopicProgressDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Training.Update);

        var progress = await progressRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(EmployeeTrainingProgress), id);

        if (input.TrainingTopicId is { } topicId)
        {
            progress.TrainingTopicId = topicId;
        }

        // Monotonic: a replayed or stale client event can never reduce recorded time.
        progress.ElapsedDurationSeconds = Math.Max(
            progress.ElapsedDurationSeconds,
            input.ElapsedDurationSeconds);

        progress.ActivePage = Math.Max(progress.ActivePage, input.ActivePage);

        progress = await progressRepository.UpdateAsync(progress, autoSave: true, cancellationToken);

        return ObjectMapper.Map<EmployeeTrainingProgress, EmployeeTrainingProgressDto>(progress);
    }

    /// <inheritdoc />
    public async Task<EmployeeTrainingProgressDto> SubmitExamAsync(
        int id,
        SubmitExamDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Training.Update);

        var progress = await progressRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(EmployeeTrainingProgress), id);

        if (input.IsFirstTest)
        {
            progress.FirstTestNote = input.Score;
            progress.FirstTestCompleted = input.IsCompleted;
        }
        else
        {
            // The final test closes a statutory training, so it is only accepted once the
            // mandatory time has actually been spent in the course.
            await EnsureMandatoryDurationSpentAsync(progress, cancellationToken);

            progress.LatestTestNote = input.Score;
            progress.LatestTestCompleted = input.IsCompleted;
        }

        progress = await progressRepository.UpdateAsync(progress, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Exam attempt recorded. ProgressId={ProgressId}, IsFirstTest={IsFirstTest}, Score={Score}",
            id, input.IsFirstTest, input.Score);

        return ObjectMapper.Map<EmployeeTrainingProgress, EmployeeTrainingProgressDto>(progress);
    }

    // -----------------------------------------------------------------

    /// <summary>
    /// Rejects a final-test submission when the employee has not yet spent the statutory
    /// training time. The required figure comes from
    /// <see cref="ITrainingPlanningManager.GetMandatoryDurationMinutes"/>; it is not restated here.
    /// </summary>
    private async Task EnsureMandatoryDurationSpentAsync(
        EmployeeTrainingProgress progress,
        CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.FindAsync(progress.CompanyEmployeeId, cancellationToken);

        if (employee is null)
        {
            return;
        }

        var company = await companyRepository.FindAsync(employee.CompanyId, cancellationToken);

        // Without a hazard class the manager has no statutory figure to apply, and there is
        // nothing to enforce.
        if (company is null || company.HazardClass == HazardClass.Unspecified)
        {
            return;
        }

        var requiredSeconds = trainingPlanningManager.GetMandatoryDurationMinutes(company.HazardClass) * 60;

        if (progress.ElapsedDurationSeconds < requiredSeconds)
        {
            throw new BusinessException(
                "The final test cannot be taken before the mandatory training time has been completed.",
                "Ensa:Training:MandatoryDurationNotCompleted")
                .WithData("RequiredMinutes", requiredSeconds / 60)
                .WithData("ElapsedMinutes", progress.ElapsedDurationSeconds / 60);
        }
    }
    /// <inheritdoc />
    public async Task<PagedResultDto<EmployeeTrainingProgressListDto>> GetListAsync(
        GetEmployeeTrainingProgressListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Training.Default);

        // Captured into locals so a single predicate covers every combination; EF folds the null
        // branches away when it translates.
        var trainingId = input.TrainingId;
        var employeeId = input.CompanyEmployeeId;
        var completed = input.LatestTestCompleted;
        var isActive = input.IsActive;

        // A progress row carries the employee, not the workplace, and the architecture allows no
        // navigation property to join through. Filtering by company therefore resolves that
        // company's employees first - one extra query - so the predicate stays in SQL and the
        // paging and total count remain correct. Filtering after paging would report the count
        // of one page as the total.
        List<int>? companyEmployeeIds = null;

        if (input.CompanyId is { } companyId)
        {
            var employeesOfCompany = await employeeRepository.GetListAsync(
                employee => employee.CompanyId == companyId, cancellationToken);

            companyEmployeeIds = [.. employeesOfCompany.Select(employee => employee.Id)];
        }

        Expression<Func<EmployeeTrainingProgress, bool>> predicate =
            progress => (trainingId == null || progress.TrainingId == trainingId)
                        && (employeeId == null || progress.CompanyEmployeeId == employeeId)
                        && (completed == null || progress.LatestTestCompleted == completed)
                        && (isActive == null || progress.IsActive == isActive)
                        && (companyEmployeeIds == null
                            || companyEmployeeIds.Contains(progress.CompanyEmployeeId));

        var sorting = NormalizeSorting(input.Sorting, "LastModificationTime DESC");

        var total = await progressRepository.GetCountAsync(predicate, cancellationToken);

        var records = await progressRepository.GetPagedListAsync(
            input.SkipCount, input.MaxResultCount, sorting, predicate, cancellationToken);

        var items = records.ConvertAll(progress => new EmployeeTrainingProgressListDto
        {
            Id = progress.Id,
            CompanyEmployeeId = progress.CompanyEmployeeId,
            TrainingId = progress.TrainingId,
            LatestTestCompleted = progress.LatestTestCompleted,
            LatestTestNote = progress.LatestTestNote,
            ElapsedDurationSeconds = progress.ElapsedDurationSeconds,
            IsActive = progress.IsActive,
            CreationTime = progress.CreationTime,
            LastModificationTime = progress.LastModificationTime,
        });

        if (items.Count > 0)
        {
            await FillNamesAsync(items, records, cancellationToken);
        }

        return new PagedResultDto<EmployeeTrainingProgressListDto>(total, items);
    }

    /// <summary>
    /// Resolves employee, workplace and training names with three batched queries for the whole
    /// page — never one per row.
    /// </summary>
    private async Task FillNamesAsync(
        List<EmployeeTrainingProgressListDto> items,
        List<EmployeeTrainingProgress> records,
        CancellationToken cancellationToken)
    {
        var employeeIds = records.Select(record => record.CompanyEmployeeId).Distinct().ToList();
        var trainingIds = records.Select(record => record.TrainingId).Distinct().ToList();

        var employees = await employeeRepository.GetListAsync(
            employee => employeeIds.Contains(employee.Id), cancellationToken);

        var trainings = await trainingRepository.GetListAsync(
            training => trainingIds.Contains(training.Id), cancellationToken);

        var companyIds = employees.Select(employee => employee.CompanyId).Distinct().ToList();

        var companies = companyIds.Count == 0
            ? []
            : await companyRepository.GetListAsync(
                company => companyIds.Contains(company.Id), cancellationToken);

        var employeeById = employees.ToDictionary(employee => employee.Id);
        var trainingNames = trainings.ToDictionary(training => training.Id, training => training.TrainingName);
        var companyNames = companies.ToDictionary(company => company.Id, company => company.CompanyName);

        for (var index = 0; index < items.Count; index++)
        {
            if (employeeById.TryGetValue(records[index].CompanyEmployeeId, out var employee))
            {
                items[index].EmployeeFullName = $"{employee.Name} {employee.LastName}".Trim();
                items[index].CompanyId = employee.CompanyId;

                if (companyNames.TryGetValue(employee.CompanyId, out var companyName))
                {
                    items[index].CompanyName = companyName;
                }
            }

            if (trainingNames.TryGetValue(records[index].TrainingId, out var trainingName))
            {
                items[index].TrainingName = trainingName;
            }
        }
    }
}
