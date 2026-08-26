using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Companies;
using Ensa.Domain.Risks;
using Ensa.Domain.Risks.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Risks;

/// <summary>
/// EF Core implementation of <see cref="ICorrectiveActionRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class CorrectiveActionRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<CorrectiveAction>(context, dataFilter), ICorrectiveActionRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// The documents (finding + result) are fetched in a single query with <c>Contains</c>; the total
    /// query count is at most 5 (corrective action, company, employee, files, source line).
    /// </remarks>
    public async Task<CorrectiveActionNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var correctiveAction = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (correctiveAction is null)
        {
            return null;
        }

        var navigation = new CorrectiveActionNavigation { CorrectiveAction = correctiveAction };

        navigation.Company = await Context.Set<Company>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == correctiveAction.CompanyId, cancellationToken);

        if (correctiveAction.OwnerCompanyEmployeeId is { } employeeId)
        {
            navigation.OwnerEmployee = await Context.Set<CompanyEmployee>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == employeeId, cancellationToken);
        }

        // Both documents in a single query.
        var documentIds = new List<int>(2);
        if (correctiveAction.FindingDocumentId is { } findingDocumentId)
        {
            documentIds.Add(findingDocumentId);
        }

        if (correctiveAction.ResultDocumentId is { } resultDocumentId)
        {
            documentIds.Add(resultDocumentId);
        }

        if (documentIds.Count > 0)
        {
            var documents = await Context.Set<Document>()
                .AsNoTracking()
                .Where(d => documentIds.Contains(d.Id))
                .ToListAsync(cancellationToken);

            navigation.FindingDocument = documents.Find(d => d.Id == correctiveAction.FindingDocumentId);
            navigation.ResultDocument = documents.Find(d => d.Id == correctiveAction.ResultDocumentId);
        }

        if (correctiveAction.FieldObservationLineId is { } lineId)
        {
            navigation.SourceFieldObservationLine = await Context.Set<FieldObservationLine>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == lineId, cancellationToken);
        }

        return navigation;
    }

    /// <inheritdoc />
    public Task<int> GetOpenCorrectiveActionCountAsync(
        int companyId,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .CountAsync(
                d => d.CompanyId == companyId && d.OperationResult == CorrectiveActionStatus.InProgress,
                cancellationToken);

    /// <inheritdoc />
    public Task<List<CorrectiveAction>> GetDeadlineOverdueAsync(
        DateTime reference,
        int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var cutoff = reference.Date;

        var query = GetReadOnlyQueryable()
            .Where(d => d.DeadlineDate != null
                        && d.DeadlineDate < cutoff
                        && d.OperationResult == CorrectiveActionStatus.InProgress);

        if (companyId is { } value)
        {
            query = query.Where(d => d.CompanyId == value);
        }

        return query
            .OrderBy(d => d.DeadlineDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<CorrectiveAction>> GetByFieldObservationLineAsync(
        int fieldObservationLineId,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .Where(d => d.FieldObservationLineId == fieldObservationLineId)
            .OrderBy(d => d.Id)
            .ToListAsync(cancellationToken);
}
