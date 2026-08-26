using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Communication;
using Ensa.Domain.Communication.Navigations;
using Ensa.Domain.Membership;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Communication;

/// <summary>
/// EF Core implementation of <see cref="IVisitRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class VisitRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<Visit>(context, dataFilter), IVisitRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// <b>INDEX:</b> the range condition is written as <c>Start &gt;= start &amp;&amp; Start &lt; end</c>
    /// — no function is applied to the date column — so that the <c>(UserId, Start, End)</c> index
    /// can be used.
    /// <para>
    /// <b>N+1 PREVENTION:</b> company and user details are fetched with one <c>IN</c> query each over the
    /// collected id sets rather than per visit, and matched up in memory (3 queries in total).
    /// </para>
    /// </remarks>
    public async Task<List<VisitNavigation>> GetCalendarAsync(
        int? userId,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {
        var query = GetReadOnlyQueryable()
            .Where(z => z.Start >= start && z.Start < end);

        if (userId is { } value)
        {
            query = query.Where(z => z.UserId == value);
        }

        var visits = await query
            .OrderBy(z => z.Start)
            .ToListAsync(cancellationToken);

        if (visits.Count == 0)
        {
            return [];
        }

        var companyIds = visits.ConvertAll(z => z.CompanyId).Distinct().ToList();

        var companies = await Context.Set<Company>()
            .AsNoTracking()
            .Where(f => companyIds.Contains(f.Id))
            .ToListAsync(cancellationToken);

        var userIds = visits.ConvertAll(z => z.UserId).Distinct().ToList();

        var users = await Context.Set<User>()
            .AsNoTracking()
            .Where(k => userIds.Contains(k.Id))
            .ToListAsync(cancellationToken);

        return visits.ConvertAll(visit => new VisitNavigation
        {
            Visit = visit,
            Company = companies.Find(f => f.Id == visit.CompanyId),
            User = users.Find(k => k.Id == visit.UserId)
        });
    }

    /// <inheritdoc />
    /// <remarks>
    /// Scanning the <c>(TenantId, CompanyId, VisitDate)</c> index backwards returns a single row.
    /// </remarks>
    public Task<Visit?> GetLatestVisitAsync(
        int companyId,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .Where(z => z.CompanyId == companyId)
            .OrderByDescending(z => z.VisitDate)
            .ThenByDescending(z => z.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
