using Ensa.Domain.Common;
using Ensa.Domain.Trainings;
using Ensa.Domain.Trainings.Navigations;
using Ensa.EntityFrameworkCore.Ambient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Trainings;

/// <summary>
/// EF Core implementation of <see cref="ITrainingRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class TrainingRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<Training>(context, dataFilter), ITrainingRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// <b>DELIBERATE FILTER BYPASS:</b> the uniqueness check must run against the organization
    /// <b>targeted</b> by <paramref name="tenantId"/>, not against the caller's current tenant context.
    /// Otherwise adding a training to an organization from a host context would report no conflict and
    /// the database uniqueness constraint would fail unexpectedly. Only the multi-tenant filter is
    /// therefore disabled through <see cref="IDataFilter.Disable{TFilter}"/>; the <b>soft-delete filter
    /// stays on</b> — the code of a deleted training must become available again.
    /// </remarks>
    public async Task<bool> CodeExistsAsync(
        string trainingCode,
        int? tenantId,
        int? exceptTrainingId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(trainingCode))
        {
            return false;
        }

        var code = trainingCode.Trim();

        using (DataFilter?.Disable<IMultiTenant>() ?? (IDisposable)DisposeAction.Empty)
        {
            return await GetReadOnlyQueryable()
                .AnyAsync(
                    e => e.TrainingCode == code
                         && e.TenantId == tenantId
                         && (exceptTrainingId == null || e.Id != exceptTrainingId),
                    cancellationToken);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>N+1 PREVENTION:</b> topic durations are fetched in a single query with <c>Contains</c> over the
    /// topic ids rather than per topic, and grouped in memory. The exams are likewise fetched in a single
    /// query using the ids taken from the <c>TrainingExam</c> link table. The total query count is at most 7
    /// regardless of the content.
    /// </remarks>
    public async Task<TrainingNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var training = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (training is null)
        {
            return null;
        }

        var navigation = new TrainingNavigation { Training = training };

        if (training.TrainingGroupId is { } groupId)
        {
            navigation.TrainingGroup = await Context.Set<TrainingGroup>()
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        }

        navigation.Durations = await Context.Set<TrainingDuration>()
            .AsNoTracking()
            .Where(s => s.TrainingId == id)
            .OrderBy(s => s.HazardClass)
            .ToListAsync(cancellationToken);

        var subjects = await Context.Set<TrainingTopic>()
            .AsNoTracking()
            .Where(k => k.TrainingId == id)
            .OrderBy(k => k.TopicOrder)
            .ToListAsync(cancellationToken);

        var topicIds = subjects.ConvertAll(k => k.Id);

        List<TrainingTopicDuration> topicDurations = topicIds.Count == 0
            ? []
            : await Context.Set<TrainingTopicDuration>()
                .AsNoTracking()
                .Where(s => topicIds.Contains(s.TrainingTopicId))
                .ToListAsync(cancellationToken);

        var durationGroups = topicDurations
            .GroupBy(s => s.TrainingTopicId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.HazardClass).ToList());

        navigation.Subjects = subjects.ConvertAll(topic => new TrainingTopicNavigation
        {
            TrainingTopic = topic,
            Durations = durationGroups.TryGetValue(topic.Id, out var list) ? list : []
        });

        var examIds = await Context.Set<TrainingExam>()
            .AsNoTracking()
            .Where(x => x.TrainingId == id && x.IsActive)
            .Select(x => x.ExamId)
            .ToListAsync(cancellationToken);

        navigation.Exams = examIds.Count == 0
            ? []
            : await Context.Set<Exam>()
                .AsNoTracking()
                .Where(s => examIds.Contains(s.Id))
                .OrderBy(s => s.Title)
                .ToListAsync(cancellationToken);

        return navigation;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The global query filter already applies <c>TenantId == CurrentTenant.Id || TenantId == null</c>,
    /// so this method only <b>narrows</b> the result with the <paramref name="tenantId"/> parameter:
    /// trainings specific to the target organization plus the host library rows shared by all
    /// organizations.
    /// </remarks>
    public Task<List<Training>> GetDefaultTrainingsAsync(
        int? tenantId,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .Where(e => e.DefaultTraining
                        && e.IsActive
                        && (e.TenantId == tenantId || e.TenantId == null))
            .OrderBy(e => e.TrainingName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<List<TrainingDuration>> GetDurationsAsync(
        int trainingId,
        CancellationToken cancellationToken = default)
        => Context.Set<TrainingDuration>()
            .AsNoTracking()
            .Where(s => s.TrainingId == trainingId)
            .OrderBy(s => s.HazardClass)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<List<TrainingTopic>> GetSubjectsAsync(
        int trainingId,
        CancellationToken cancellationToken = default)
        => Context.Set<TrainingTopic>()
            .AsNoTracking()
            .Where(k => k.TrainingId == trainingId)
            .OrderBy(k => k.TopicOrder)
            .ThenBy(k => k.Id)
            .ToListAsync(cancellationToken);
}
