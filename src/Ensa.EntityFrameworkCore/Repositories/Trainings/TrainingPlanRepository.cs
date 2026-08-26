using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Trainings;
using Ensa.Domain.Trainings.Navigations;
using Ensa.Domain.Companies;
using Ensa.Domain.Membership;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Trainings;

/// <summary>
/// EF Core implementation of <see cref="ITrainingPlanRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class TrainingPlanRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<TrainingPlan>(context, dataFilter), ITrainingPlanRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// The year is compared with a half-open date range rather than <c>YEAR(StartDate)</c>,
    /// so that the <c>(CompanyId, StartDate)</c> index can be used.
    /// </remarks>
    public Task<TrainingPlan?> GetActivePlanAsync(
        int companyId,
        int year,
        CancellationToken cancellationToken = default)
    {
        var yearPer = new DateTime(year, 1, 1);
        var nextYearPer = yearPer.AddYears(1);

        return GetReadOnlyQueryable()
            .Where(p => p.CompanyId == companyId
                        && p.IsActive
                        && p.StartDate >= yearPer
                        && p.StartDate < nextYearPer)
            .OrderByDescending(p => p.StartDate)
            .ThenByDescending(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>N+1 PREVENTION:</b> the training names, trainer users and evidence document names of the lines
    /// are fetched with one <c>Contains</c> query each over the collected id sets rather than per line.
    /// The total query count is at most 7 regardless of the number of lines.
    /// </remarks>
    public async Task<TrainingPlanNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (plan is null)
        {
            return null;
        }

        var navigation = new TrainingPlanNavigation { TrainingPlan = plan };

        navigation.Company = await Context.Set<Company>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == plan.CompanyId, cancellationToken);

        var lines = await Context.Set<TrainingPlanLine>()
            .AsNoTracking()
            .Where(s => s.TrainingPlanId == id)
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Month)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        // Every user id on the plan header and its lines is resolved in a SINGLE query.
        var userIds = new List<int>();
        Add(userIds, plan.SpecialistUserId);
        Add(userIds, plan.PhysicianUserId);
        Add(userIds, plan.ApproverUserId);
        foreach (var line in lines)
        {
            Add(userIds, line.InstructorUserId);
        }

        List<User> users = userIds.Count == 0
            ? []
            : await Context.Set<User>()
                .AsNoTracking()
                .Where(k => userIds.Contains(k.Id))
                .ToListAsync(cancellationToken);

        navigation.Specialist = users.Find(k => k.Id == plan.SpecialistUserId);
        navigation.Physician = users.Find(k => k.Id == plan.PhysicianUserId);
        navigation.Approver = users.Find(k => k.Id == plan.ApproverUserId);

        // Training names in a SINGLE query (no lookup per row).
        var trainingIds = lines.ConvertAll(s => s.TrainingId).Distinct().ToList();

        List<NameLine> trainingNames = trainingIds.Count == 0
            ? []
            : await Context.Set<Training>()
                .AsNoTracking()
                .Where(e => trainingIds.Contains(e.Id))
                .Select(e => new NameLine(e.Id, e.TrainingName))
                .ToListAsync(cancellationToken);

        // Evidence document names in a SINGLE query as well.
        var documentIds = lines
            .Where(s => s.DocumentId.HasValue)
            .Select(s => s.DocumentId!.Value)
            .Distinct()
            .ToList();

        List<NameLine> documentNames = documentIds.Count == 0
            ? []
            : await Context.Set<Document>()
                .AsNoTracking()
                .Where(d => documentIds.Contains(d.Id))
                .Select(d => new NameLine(d.Id, d.DocumentName))
                .ToListAsync(cancellationToken);

        navigation.Lines = lines.ConvertAll(line => new TrainingPlanLineNavigation
        {
            TrainingPlanLine = line,
            TrainingName = trainingNames.Find(e => e.Id == line.TrainingId)?.Name ?? string.Empty,
            InstructorUser = users.Find(k => k.Id == line.InstructorUserId),
            DocumentName = documentNames.Find(d => d.Id == line.DocumentId)?.Name
        });

        return navigation;
    }

    /// <inheritdoc />
    public Task<List<TrainingPlanLine>> GetIncompleteLinesAsync(
        int trainingPlanId,
        CancellationToken cancellationToken = default)
        => Context.Set<TrainingPlanLine>()
            .AsNoTracking()
            .Where(s => s.TrainingPlanId == trainingPlanId
                        && s.IsActive
                        && s.Status != PlanLineStatus.Completed)
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Month)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<List<TrainingPlanLine>> GetLinesAsync(
        int trainingPlanId,
        CancellationToken cancellationToken = default)
        => Context.Set<TrainingPlanLine>()
            .AsNoTracking()
            .Where(s => s.TrainingPlanId == trainingPlanId)
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Month)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

    private static void Add(List<int> target, int? value)
    {
        if (value is { } id && !target.Contains(id))
        {
            target.Add(id);
        }
    }

    /// <summary>Lightweight lookup projection carrying an id and a name.</summary>
    private sealed record NameLine(int Id, string Name);
}
