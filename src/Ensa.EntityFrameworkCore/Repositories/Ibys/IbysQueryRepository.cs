using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Ibys;
using Ensa.Domain.Ibys.Navigations;
using Ensa.Domain.Membership;
using Ensa.Domain.Health;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Ibys;

/// <summary>
/// EF Core implementation of <see cref="IIbysQueryRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class IbysQueryRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<IbysQuery>(context, dataFilter), IIbysQueryRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// The linked examination forms are fetched with a <b>single</b> query; no extra query is issued per
    /// form. The total query count is at most 5.
    /// </remarks>
    public async Task<IbysQueryNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var query = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (query is null)
        {
            return null;
        }

        var navigation = new IbysQueryNavigation { Query = query };

        if (query.CompanyId is { } companyId)
        {
            navigation.Company = await Context.Set<Company>()
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == companyId, cancellationToken);
        }

        if (query.CompanyEmployeeId is { } employeeId)
        {
            navigation.Employee = await Context.Set<CompanyEmployee>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == employeeId, cancellationToken);
        }

        navigation.ExaminationForms = await Context.Set<MedicalExaminationForm>()
            .AsNoTracking()
            .Where(f => f.IbysQueryId == id)
            .OrderBy(f => f.ExaminationDate)
            .ToListAsync(cancellationToken);

        // IbysQuery has no separate "approver" foreign key; the user who created and submitted
        // the notification with an e-signature is represented by the CreatorId audit field.
        if (query.CreatorId is { } approverId)
        {
            navigation.ApproverFullName = await Context.Set<UserProfile>()
                .AsNoTracking()
                .Where(k => k.UserId == approverId)
                .Select(k => k.Name + " " + k.LastName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return navigation;
    }

    /// <inheritdoc />
    /// <remarks>
    /// "Awaiting a result" means rows that were submitted but have not reached a final state
    /// (<see cref="IbysSubmissionStatus.Approved"/>, <see cref="IbysSubmissionStatus.Failed"/>,
    /// <see cref="IbysSubmissionStatus.Cancelled"/>). The oldest submission is queried first and the
    /// result set is capped in the database with <c>Take</c>.
    /// </remarks>
    public Task<List<IbysQuery>> GetPendingAsync(
        IbysQueryType type,
        int maxResultCount = 100,
        CancellationToken cancellationToken = default)
    {
        var takeCount = Math.Clamp(maxResultCount, 1, 1000);

        return GetReadOnlyQueryable()
            .Where(s => s.QueryType == type && s.Status == IbysSubmissionStatus.Sent)
            .OrderBy(s => s.SubmissionDate)
            .ThenBy(s => s.Id)
            .Take(takeCount)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<IbysQuery?> FindByQueryNoAsync(
        string queryNo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queryNo))
        {
            return Task.FromResult<IbysQuery?>(null);
        }

        var value = queryNo.Trim();

        return GetReadOnlyQueryable()
            .FirstOrDefaultAsync(s => s.QueryNo == value, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<IbysQuery>> GetByGroupIdAsync(
        string groupId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            return Task.FromResult(new List<IbysQuery>());
        }

        var value = groupId.Trim();

        return GetReadOnlyQueryable()
            .Where(s => s.GroupId == value)
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);
    }
}
