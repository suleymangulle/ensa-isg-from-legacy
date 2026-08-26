using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Companies;
using Ensa.Domain.Membership;
using Ensa.Domain.Plans;
using Ensa.Domain.Plans.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Plans;

/// <summary>
/// EF Core implementation of <see cref="IWorkPlanRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class WorkPlanRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<WorkPlan>(context, dataFilter), IWorkPlanRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// The year is compared with a half-open date range rather than <c>YEAR(StartDate)</c>,
    /// so that the <c>(CompanyId, StartDate)</c> index can be used.
    /// </remarks>
    public Task<WorkPlan?> GetActivePlanAsync(
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
    /// <b>N+1 PREVENTION:</b> the activity names, trainer users and evidence document names of the lines
    /// are fetched with one <c>Contains</c> query each over the collected id sets rather than per line.
    /// The total query count is at most 7 regardless of the number of lines.
    /// </remarks>
    public async Task<WorkPlanNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (plan is null)
        {
            return null;
        }

        var navigation = new WorkPlanNavigation { WorkPlan = plan };

        navigation.Company = await Context.Set<Company>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == plan.CompanyId, cancellationToken);

        var lines = await Context.Set<WorkPlanLine>()
            .AsNoTracking()
            .Where(s => s.WorkPlanId == id)
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

        // Activity names in a SINGLE query (no lookup per row).
        var activityIds = lines.ConvertAll(s => s.ActivityId).Distinct().ToList();

        List<NameLine> activityNames = activityIds.Count == 0
            ? []
            : await Context.Set<Activity>()
                .AsNoTracking()
                .Where(a => activityIds.Contains(a.Id))
                .Select(a => new NameLine(a.Id, a.ActivityName))
                .ToListAsync(cancellationToken);

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

        navigation.Lines = lines.ConvertAll(line => new WorkPlanLineNavigation
        {
            WorkPlanLine = line,
            ActivityName = activityNames.Find(a => a.Id == line.ActivityId)?.Name ?? string.Empty,
            InstructorUser = users.Find(k => k.Id == line.InstructorUserId),
            DocumentName = documentNames.Find(d => d.Id == line.DocumentId)?.Name
        });

        return navigation;
    }

    /// <inheritdoc />
    public Task<List<WorkPlanLine>> GetApprovalPendingLinesAsync(
        int workPlanId,
        CancellationToken cancellationToken = default)
        => Context.Set<WorkPlanLine>()
            .AsNoTracking()
            .Where(s => s.WorkPlanId == workPlanId
                        && s.IsActive
                        && s.ApprovalStatus == ApprovalStatus.SubmittedForApproval)
            .OrderBy(s => s.ForApprovalSendingDate)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// The ratio is computed <b>in the database</b> with a single query: the rows are not loaded into
    /// memory, only the total and the "done" counts are read. When there are no rows it returns 0 (no
    /// division by zero).
    /// </remarks>
    public async Task<double> GetCompletionRateAsync(
        int workPlanId,
        CancellationToken cancellationToken = default)
    {
        var summary = await Context.Set<WorkPlanLine>()
            .AsNoTracking()
            .Where(s => s.WorkPlanId == workPlanId && s.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Yapilan = g.Count(s => s.Status == PlanLineStatus.Completed)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (summary is null || summary.Total == 0)
        {
            return 0d;
        }

        return (double)summary.Yapilan / summary.Total;
    }

    /// <inheritdoc />
    public Task<List<WorkPlanLine>> GetLinesAsync(
        int workPlanId,
        CancellationToken cancellationToken = default)
        => Context.Set<WorkPlanLine>()
            .AsNoTracking()
            .Where(s => s.WorkPlanId == workPlanId)
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
