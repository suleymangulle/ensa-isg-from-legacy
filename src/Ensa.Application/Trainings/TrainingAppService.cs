using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Trainings;
using Ensa.Application.Contracts.Trainings.Dtos;
using Ensa.Application.Contracts.Trainings.Dtos.Navigations;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Ensa.Domain.Trainings;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Trainings;

/// <summary>
/// Training catalogue application service.
/// <para>
/// Hazard-class durations are exposed and accepted as a list of
/// <c>{ hazardClass, durationMinutes }</c> pairs — for the training and for each of its
/// topics. The legacy three-column shape is gone; every save replaces the whole set.
/// </para>
/// <para>
/// The statutory refresh interval and the mandatory duration belong to
/// <see cref="ITrainingPlanningManager"/>. That manager only calculates — it performs no
/// persistence — so this service saves the entities itself.
/// </para>
/// </summary>
public class TrainingAppService(
    IServiceProvider serviceProvider,
    ITrainingRepository trainingRepository,
    ITrainingPlanningManager trainingPlanningManager,
    IRepository<TrainingDuration> durationRepository,
    IRepository<TrainingTopic> topicRepository,
    IRepository<TrainingTopicDuration> topicDurationRepository)
    : EnsaAppService(serviceProvider), ITrainingAppService
{
    /// <summary>Maximum number of records returned by a drop-down lookup.</summary>
    private const int LookupMaxRecord = 50;

    /// <inheritdoc />
    public async Task<TrainingDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Training.Default);

        var training = await trainingRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Training), id);

        var dto = ObjectMapper.Map<Training, TrainingDto>(training);

        var durations = await trainingRepository.GetDurationsAsync(id, cancellationToken);
        dto.Durations = ObjectMapper.Map<List<TrainingDuration>, List<TrainingDurationDto>>(durations);

        return dto;
    }

    /// <inheritdoc />
    public async Task<TrainingNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Training.Default);

        // A single repository call returns the training, its group, its durations, every
        // topic with that topic's own durations, and the attached exams. Nothing below
        // issues a query per topic.
        var navigation = await trainingRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(Training), id);

        var trainingDto = ObjectMapper.Map<Training, TrainingDto>(navigation.Training);
        trainingDto.Durations =
            ObjectMapper.Map<List<TrainingDuration>, List<TrainingDurationDto>>(navigation.Durations);

        return new TrainingNavigationDto
        {
            Training = trainingDto,
            TrainingGroup = navigation.TrainingGroup is null
                ? null
                : new LookupDto
                {
                    Id = navigation.TrainingGroup.Id,
                    DisplayName = navigation.TrainingGroup.TrainingGroupName,
                    Code = navigation.TrainingGroup.TrainingGroupCode
                },
            Topics =
            [
                .. navigation.Subjects
                    .OrderBy(t => t.TrainingTopic.TopicOrder)
                    .Select(t =>
                    {
                        var topicDto = ObjectMapper.Map<TrainingTopic, TrainingTopicDto>(t.TrainingTopic);
                        topicDto.Durations = ObjectMapper
                            .Map<List<TrainingTopicDuration>, List<TrainingTopicDurationDto>>(t.Durations);
                        return topicDto;
                    })
            ],
            Exams =
            [
                .. navigation.Exams.Select(e => new LookupDto
                {
                    Id = e.Id,
                    DisplayName = e.Title,
                    IsActive = e.IsActive
                })
            ]
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<TrainingListDto>> GetListAsync(
        GetTrainingListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Training.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "TrainingName ASC");

        var total = await trainingRepository.GetCountAsync(predicate, cancellationToken);

        var records = await trainingRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Training>, List<TrainingListDto>>(records);

        return new PagedResultDto<TrainingListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Training.Default);

        var search = filter?.Trim();

        var records = await trainingRepository.GetPagedListAsync(
            skipCount: 0,
            maxResultCount: LookupMaxRecord,
            sorting: "TrainingName ASC",
            predicate: string.IsNullOrEmpty(search)
                ? t => t.IsActive
                : t => t.IsActive && t.TrainingName.Contains(search),
            cancellationToken);

        var items = records
            .Select(t => new LookupDto
            {
                Id = t.Id,
                DisplayName = t.TrainingName,
                Code = t.TrainingCode,
                IsActive = t.IsActive
            })
            .ToList();

        return new ListResultDto<LookupDto>(items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<TrainingListDto>> GetDefaultsAsync(
        int? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Training.Default);

        var records = await trainingRepository.GetDefaultTrainingsAsync(
            tenantId ?? CurrentTenant.Id,
            cancellationToken);

        return new ListResultDto<TrainingListDto>(
            ObjectMapper.Map<List<Training>, List<TrainingListDto>>(records));
    }

    /// <inheritdoc />
    public async Task<TrainingDto> CreateAsync(
        CreateTrainingDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Training.Create);

        ValidateDurations(input.Durations.Select(d => d.HazardClass));

        if (!string.IsNullOrWhiteSpace(input.TrainingCode)
            && await trainingRepository.CodeExistsAsync(
                input.TrainingCode.Trim(),
                CurrentTenant.Id,
                exceptTrainingId: null,
                cancellationToken))
        {
            throw new BusinessException(
                "This training code is already in use.",
                "Ensa:Training:CodeAlreadyUsed")
                .WithData("TrainingCode", input.TrainingCode.Trim());
        }

        var training = ObjectMapper.Map<CreateTrainingDto, Training>(input);
        training.IsActive = true;

        // ITrainingPlanningManager only calculates; nothing is persisted by it, so the
        // training is saved here, exactly once.
        training = await trainingRepository.InsertAsync(training, autoSave: true, cancellationToken);

        await ReplaceDurationsAsync(training.Id, input.Durations, cancellationToken);

        Logger.LogInformation(
            "Training created. TrainingId={TrainingId}, Name={TrainingName}", training.Id, training.TrainingName);

        return await GetAsync(training.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TrainingDto> UpdateAsync(
        int id,
        UpdateTrainingDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Training.Update);

        var training = await trainingRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Training), id);

        ValidateDurations(input.Durations.Select(d => d.HazardClass));

        if (!string.IsNullOrWhiteSpace(input.TrainingCode)
            && await trainingRepository.CodeExistsAsync(
                input.TrainingCode.Trim(),
                training.TenantId,
                exceptTrainingId: id,
                cancellationToken))
        {
            throw new BusinessException(
                "This training code is already in use.",
                "Ensa:Training:CodeAlreadyUsed")
                .WithData("TrainingCode", input.TrainingCode.Trim());
        }

        ObjectMapper.Map(input, training);

        await trainingRepository.UpdateAsync(training, autoSave: true, cancellationToken);

        await ReplaceDurationsAsync(id, input.Durations, cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Training.Delete);

        var training = await trainingRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Training), id);

        var topics = await trainingRepository.GetSubjectsAsync(id, cancellationToken);

        if (topics.Count > 0)
        {
            // One statement removes every topic duration of the training — a per-topic
            // delete would issue one round trip per topic.
            var topicIds = topics.Select(t => t.Id).ToList();

            await topicDurationRepository.DeleteDirectAsync(
                d => topicIds.Contains(d.TrainingTopicId),
                cancellationToken);

            await topicRepository.DeleteManyAsync(topics, autoSave: false, cancellationToken);
        }

        await durationRepository.DeleteDirectAsync(d => d.TrainingId == id, cancellationToken);

        await trainingRepository.DeleteAsync(training, autoSave: true, cancellationToken);

        Logger.LogInformation("Training deleted. TrainingId={TrainingId}", id);
    }

    // ------------------------------------------------------------------ Topics

    /// <inheritdoc />
    public async Task<ListResultDto<TrainingTopicDto>> GetTopicsAsync(
        int trainingId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Training.Default);

        _ = await trainingRepository.FindAsync(trainingId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Training), trainingId);

        var topics = await trainingRepository.GetSubjectsAsync(trainingId, cancellationToken);

        var items = ObjectMapper.Map<List<TrainingTopic>, List<TrainingTopicDto>>(topics);

        if (items.Count == 0)
        {
            return new ListResultDto<TrainingTopicDto>(items);
        }

        // One query for every topic duration in the training, then an in-memory group —
        // not one query per topic.
        var topicIds = topics.Select(t => t.Id).ToList();

        var durations = await topicDurationRepository.GetListAsync(
            d => topicIds.Contains(d.TrainingTopicId),
            cancellationToken);

        var byTopic = durations
            .GroupBy(d => d.TrainingTopicId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var item in items)
        {
            if (byTopic.TryGetValue(item.Id, out var topicDurations))
            {
                item.Durations = ObjectMapper
                    .Map<List<TrainingTopicDuration>, List<TrainingTopicDurationDto>>(topicDurations);
            }
        }

        return new ListResultDto<TrainingTopicDto>([.. items.OrderBy(t => t.TopicOrder)]);
    }

    /// <inheritdoc />
    public async Task<TrainingTopicDto> CreateTopicAsync(
        int trainingId,
        CreateTrainingTopicDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Training.Create);

        _ = await trainingRepository.FindAsync(trainingId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Training), trainingId);

        ValidateDurations(input.Durations.Select(d => d.HazardClass));

        var topic = ObjectMapper.Map<CreateTrainingTopicDto, TrainingTopic>(input);
        topic.TrainingId = trainingId;

        topic = await topicRepository.InsertAsync(topic, autoSave: true, cancellationToken);

        await ReplaceTopicDurationsAsync(topic.Id, input.Durations, cancellationToken);

        return await GetTopicAsync(topic.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TrainingTopicDto> UpdateTopicAsync(
        int trainingId,
        int topicId,
        UpdateTrainingTopicDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Training.Update);

        var topic = await FindOwnedTopicAsync(trainingId, topicId, cancellationToken);

        ValidateDurations(input.Durations.Select(d => d.HazardClass));

        ObjectMapper.Map(input, topic);
        topic.TrainingId = trainingId;

        await topicRepository.UpdateAsync(topic, autoSave: true, cancellationToken);

        await ReplaceTopicDurationsAsync(topicId, input.Durations, cancellationToken);

        return await GetTopicAsync(topicId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteTopicAsync(int trainingId, int topicId, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Training.Delete);

        var topic = await FindOwnedTopicAsync(trainingId, topicId, cancellationToken);

        await topicDurationRepository.DeleteDirectAsync(d => d.TrainingTopicId == topicId, cancellationToken);

        await topicRepository.DeleteAsync(topic, autoSave: true, cancellationToken);
    }

    // ------------------------------------------------- Statutory calculations

    /// <inheritdoc />
    public async Task<TrainingValidityDto> GetValidityAsync(
        int companyEmployeeId,
        int trainingId,
        HazardClass hazardClass,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Training.Default);

        _ = await trainingRepository.FindAsync(trainingId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Training), trainingId);

        // Both answers come from the manager; the refresh interval and the 480/720/960
        // minute figures are never restated here.
        var isValid = await trainingPlanningManager.IsTrainingValidAsync(
            companyEmployeeId,
            trainingId,
            hazardClass,
            cancellationToken);

        return new TrainingValidityDto
        {
            CompanyEmployeeId = companyEmployeeId,
            TrainingId = trainingId,
            HazardClass = hazardClass,
            IsValid = isValid,
            MandatoryDurationMinutes = trainingPlanningManager.GetMandatoryDurationMinutes(hazardClass)
        };
    }

    // -----------------------------------------------------------------

    private async Task<TrainingTopic> FindOwnedTopicAsync(
        int trainingId,
        int topicId,
        CancellationToken cancellationToken)
    {
        var topic = await topicRepository.FindAsync(topicId, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(TrainingTopic), topicId);

        if (topic.TrainingId != trainingId)
        {
            throw new EntityNotFoundException(typeof(TrainingTopic), topicId);
        }

        return topic;
    }

    private async Task<TrainingTopicDto> GetTopicAsync(int topicId, CancellationToken cancellationToken)
    {
        var topic = await topicRepository.FindAsync(topicId, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(TrainingTopic), topicId);

        var dto = ObjectMapper.Map<TrainingTopic, TrainingTopicDto>(topic);

        var durations = await topicDurationRepository.GetListAsync(
            d => d.TrainingTopicId == topicId,
            cancellationToken);

        dto.Durations = ObjectMapper.Map<List<TrainingTopicDuration>, List<TrainingTopicDurationDto>>(durations);

        return dto;
    }

    /// <summary>Rejects a duration set that names the same hazard class twice or leaves it unspecified.</summary>
    private static void ValidateDurations(IEnumerable<HazardClass> hazardClasses)
    {
        var classes = hazardClasses.ToList();

        if (classes.Exists(c => c == HazardClass.Unspecified))
        {
            throw new BusinessException(
                "A training duration must name a hazard class.",
                "Ensa:Training:DurationHazardClassRequired");
        }

        var duplicate = classes.GroupBy(c => c).FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            throw new BusinessException(
                "A training may hold only one duration per hazard class.",
                "Ensa:Training:DuplicateDuration")
                .WithData("HazardClass", duplicate.Key);
        }
    }

    /// <summary>Replaces the hazard-class duration set of a training.</summary>
    private async Task ReplaceDurationsAsync(
        int trainingId,
        List<SaveTrainingDurationDto> durations,
        CancellationToken cancellationToken)
    {
        // Physical delete: the table is unique on (training, hazard class), so soft-deleted
        // predecessors would collide with the replacement rows.
        await durationRepository.DeleteDirectAsync(d => d.TrainingId == trainingId, cancellationToken);

        if (durations.Count == 0)
        {
            return;
        }

        var rows = durations
            .Select(d => new TrainingDuration
            {
                TrainingId = trainingId,
                HazardClass = d.HazardClass,
                DurationMinutes = d.DurationMinutes
            })
            .ToList();

        await durationRepository.InsertManyAsync(rows, autoSave: true, cancellationToken);
    }

    /// <summary>Replaces the hazard-class duration set of a training topic.</summary>
    private async Task ReplaceTopicDurationsAsync(
        int topicId,
        List<SaveTrainingTopicDurationDto> durations,
        CancellationToken cancellationToken)
    {
        await topicDurationRepository.DeleteDirectAsync(d => d.TrainingTopicId == topicId, cancellationToken);

        if (durations.Count == 0)
        {
            return;
        }

        var rows = durations
            .Select(d => new TrainingTopicDuration
            {
                TrainingTopicId = topicId,
                HazardClass = d.HazardClass,
                DurationMinutes = d.DurationMinutes
            })
            .ToList();

        await topicDurationRepository.InsertManyAsync(rows, autoSave: true, cancellationToken);
    }

    private static Expression<Func<Training, bool>>? BuildFilter(GetTrainingListInput input)
    {
        Expression<Func<Training, bool>> predicate = t => true;
        var applied = false;

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var search = input.Filter.Trim();
            predicate = Combine(predicate, t =>
                t.TrainingName.Contains(search)
                || (t.TrainingCode != null && t.TrainingCode.Contains(search)));
            applied = true;
        }

        if (input.TrainingGroupId is { } groupId)
        {
            predicate = Combine(predicate, t => t.TrainingGroupId == groupId);
            applied = true;
        }

        if (input.TrainingType is { } trainingType)
        {
            predicate = Combine(predicate, t => t.TrainingType == trainingType);
            applied = true;
        }

        if (input.TopicGroup is { } topicGroup)
        {
            predicate = Combine(predicate, t => t.TopicGroup == topicGroup);
            applied = true;
        }

        if (input.MandatoryTraining is { } mandatory)
        {
            predicate = Combine(predicate, t => t.MandatoryTraining == mandatory);
            applied = true;
        }

        if (input.DefaultTraining is { } isDefault)
        {
            predicate = Combine(predicate, t => t.DefaultTraining == isDefault);
            applied = true;
        }

        if (input.IsActive is { } isActive)
        {
            predicate = Combine(predicate, t => t.IsActive == isActive);
            applied = true;
        }

        return applied ? predicate : null;
    }

    private static Expression<Func<Training, bool>> Combine(
        Expression<Func<Training, bool>> left,
        Expression<Func<Training, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(Training), "t");

        var body = Expression.AndAlso(
            new ParameterRebinder(left.Parameters[0], parameter).Visit(left.Body)!,
            new ParameterRebinder(right.Parameters[0], parameter).Visit(right.Body)!);

        return Expression.Lambda<Func<Training, bool>>(body, parameter);
    }

    /// <summary>Rewrites two separate lambdas onto a single shared parameter.</summary>
    private sealed class ParameterRebinder(ParameterExpression previous, ParameterExpression replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == previous ? replacement : base.VisitParameter(node);
    }
}
