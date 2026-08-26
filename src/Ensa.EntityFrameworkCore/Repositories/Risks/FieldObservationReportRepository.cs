using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Companies;
using Ensa.Domain.Risks;
using Ensa.Domain.Risks.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Risks;

/// <summary>
/// EF Core implementation of <see cref="IFieldObservationReportRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class FieldObservationReportRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<FieldObservationReport>(context, dataFilter), IFieldObservationReportRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// <b>N+1 PREVENTION:</b> the files, responsible employees and derived corrective actions of the lines
    /// are fetched in bulk with one <c>Contains</c> query each rather than per line, and grouped in memory.
    /// The total query count is at most 7 regardless of the number of lines.
    /// </remarks>
    public async Task<FieldObservationReportNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var report = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report is null)
        {
            return null;
        }

        var navigation = new FieldObservationReportNavigation { Report = report };

        navigation.Company = await Context.Set<Company>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == report.CompanyId, cancellationToken);

        if (report.DepartmentId is { } departmentId)
        {
            navigation.Department = await Context.Set<WorkplaceDepartment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == departmentId, cancellationToken);
        }

        var lines = await Context.Set<FieldObservationLine>()
            .AsNoTracking()
            .Where(s => s.FieldObservationReportId == id)
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

        var lineIds = lines.ConvertAll(s => s.Id);

        var documentIds = lines
            .Where(s => s.DocumentId.HasValue)
            .Select(s => s.DocumentId!.Value)
            .Distinct()
            .ToList();

        List<Document> documents = documentIds.Count == 0
            ? []
            : await Context.Set<Document>()
                .AsNoTracking()
                .Where(d => documentIds.Contains(d.Id))
                .ToListAsync(cancellationToken);

        var employeeIds = lines
            .Where(s => s.OwnerCompanyEmployeeId.HasValue)
            .Select(s => s.OwnerCompanyEmployeeId!.Value)
            .Distinct()
            .ToList();

        List<CompanyEmployee> employees = employeeIds.Count == 0
            ? []
            : await Context.Set<CompanyEmployee>()
                .AsNoTracking()
                .Where(p => employeeIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

        List<CorrectiveAction> correctiveActions = lineIds.Count == 0
            ? []
            : await Context.Set<CorrectiveAction>()
                .AsNoTracking()
                .Where(d => d.FieldObservationLineId != null
                            && lineIds.Contains(d.FieldObservationLineId!.Value))
                .ToListAsync(cancellationToken);

        var correctiveActionGroups = correctiveActions
            .GroupBy(d => d.FieldObservationLineId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        navigation.Lines = lines.ConvertAll(line => new FieldObservationLineNavigation
        {
            Line = line,
            Document = documents.Find(d => d.Id == line.DocumentId),
            OwnerEmployee = employees.Find(p => p.Id == line.OwnerCompanyEmployeeId),
            CorrectiveActions = correctiveActionGroups.TryGetValue(line.Id, out var list) ? list : []
        });

        return navigation;
    }

    /// <inheritdoc />
    public Task<List<FieldObservationReport>> GetListByCompanyAsync(
        int companyId,
        DateTime? start = null,
        DateTime? end = null,
        int? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetReadOnlyQueryable().Where(r => r.CompanyId == companyId);

        if (start is { } startValue)
        {
            var lowerBound = startValue.Date;
            query = query.Where(r => r.Date >= lowerBound);
        }

        if (end is { } endValue)
        {
            var upperBound = endValue.Date.AddDays(1);
            query = query.Where(r => r.Date < upperBound);
        }

        if (departmentId is { } department)
        {
            query = query.Where(r => r.DepartmentId == department);
        }

        return query
            .OrderByDescending(r => r.Date)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<FieldObservationLine>> GetLinesAsync(
        int fieldObservationReportId,
        CancellationToken cancellationToken = default)
        => Context.Set<FieldObservationLine>()
            .AsNoTracking()
            .Where(s => s.FieldObservationReportId == fieldObservationReportId)
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// "No closed corrective action" is interpreted as the line having no corrective action in the
    /// <see cref="CorrectiveActionStatus.Closed"/> state. The subquery is translated to <c>NOT EXISTS</c>
    /// and produces no extra query per row.
    /// </remarks>
    public Task<List<FieldObservationLine>> GetDeadlineElapsedLinesAsync(
        DateTime reference,
        int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var cutoff = reference.Date;

        var correctiveActions = Context.Set<CorrectiveAction>();

        var query = Context.Set<FieldObservationLine>()
            .AsNoTracking()
            .Where(s => s.DeadlineDate != null
                        && s.DeadlineDate < cutoff
                        && !correctiveActions.Any(d => d.FieldObservationLineId == s.Id
                                            && d.OperationResult == CorrectiveActionStatus.Closed));

        if (companyId is { } value)
        {
            // The line carries no CompanyId; the company link goes through the report header.
            var reports = Context.Set<FieldObservationReport>();
            query = query.Where(s => reports.Any(r => r.Id == s.FieldObservationReportId
                                                       && r.CompanyId == value));
        }

        return query
            .OrderBy(s => s.DeadlineDate)
            .ToListAsync(cancellationToken);
    }
}
