using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Companies.Navigations;
using Ensa.Domain.Membership;
using Ensa.Domain.Lookups;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Companies;

/// <summary>
/// Queries specific to the <see cref="Company"/> module.
/// <para>
/// <see cref="Company"/> implements both <c>IMultiTenant</c> and <c>ISoftDelete</c>; every query goes
/// through the global query filters, so the <c>TenantId</c> / <c>IsDeleted</c> predicate is never written by
/// hand. <see cref="City"/>, <see cref="District"/> and <see cref="Neighborhood"/> on the other hand are
/// host reference tables.
/// </para>
/// </summary>
public class CompanyRepository(EnsaDbContext context, IDataFilter dataFilter)
    : EfCoreRepository<Company>(context, dataFilter), ICompanyRepository
{
    /// <summary>
    /// Returns the company in its combined representation.
    /// <para>
    /// <b>N+1 prevention:</b> because there are no navigation properties, the parts are collected with
    /// separate queries; the query count is nevertheless <b>constant</b> (independent of the number of
    /// records). Collections are fetched with a single <c>WHERE ... IN (...)</c> query and matched up in
    /// memory.
    /// </para>
    /// </summary>
    public async Task<CompanyNavigation?> GetWithNavigationAsync(
        int id,
        bool includeEmployees = true,
        bool includeBranches = true,
        CancellationToken cancellationToken = default)
    {
        var company = await GetReadOnlyQueryable().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (company is null)
        {
            return null;
        }

        var navigation = new CompanyNavigation { Company = company };

        // ---- Address lookups (host reference tables) ----
        navigation.City = await Context.Set<City>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == company.CityId, cancellationToken);

        navigation.District = await Context.Set<District>()
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == company.DistrictId, cancellationToken);

        if (company.NeighborhoodId is int neighborhoodId)
        {
            navigation.Neighborhood = await Context.Set<Neighborhood>()
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == neighborhoodId, cancellationToken);
        }

        // ---- Headquarters / branch ----
        if (company.HeadquarterCompanyId is int headquarterId)
        {
            navigation.HeadquarterCompany = await GetReadOnlyQueryable()
                .FirstOrDefaultAsync(f => f.Id == headquarterId, cancellationToken);
        }

        if (includeBranches)
        {
            navigation.Branches = await GetBranchesAsync(id, onlyActive: false, cancellationToken);
        }

        // ---- Child collections ----
        if (includeEmployees)
        {
            navigation.Employees = await Context.Set<CompanyEmployee>()
                .AsNoTracking()
                .Where(p => p.CompanyId == id)
                .OrderBy(p => p.Name)
                .ThenBy(p => p.LastName)
                .ToListAsync(cancellationToken);
        }

        navigation.AssignedSpecialists = await Context.Set<AssignedSpecialist>()
            .AsNoTracking()
            .Where(fi => fi.CompanyId == id && fi.IsActive)
            .ToListAsync(cancellationToken);

        // The national ids of the involved people are collected in a SINGLE query (no query per person).
        var assignedSpecialistUserIds = navigation.AssignedSpecialists.Select(fi => fi.UserId).Distinct().ToList();
        if (assignedSpecialistUserIds.Count > 0)
        {
            navigation.RelatedTcMembershipNumbers = await Context.Set<UserProfile>()
                .AsNoTracking()
                .Where(p => assignedSpecialistUserIds.Contains(p.UserId) && p.NationalId != null)
                .Select(p => p.NationalId!)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        navigation.Departments = await Context.Set<WorkplaceDepartment>()
            .AsNoTracking()
            .Where(b => b.CompanyId == id)
            .OrderBy(b => b.DepartmentName)
            .ToListAsync(cancellationToken);

        // ---- Summary ----
        navigation.Warning = await Context.Set<CompanyComplianceSummary>()
            .AsNoTracking()
            .Where(u => u.CompanyId == id)
            .OrderByDescending(u => u.CalculatedTime)
            .ThenByDescending(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return navigation;
    }

    /// <inheritdoc />
    public Task<List<Company>> GetBranchesAsync(
        int headquarterCompanyId,
        bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var query = GetReadOnlyQueryable().Where(f => f.HeadquarterCompanyId == headquarterCompanyId);

        if (onlyActive)
        {
            query = query.Where(f => f.IsActive);
        }

        return query
            .OrderBy(f => f.BranchNo)
            .ThenBy(f => f.CompanyName)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns a paged list of the companies assigned to the given user.
    /// <para>
    /// The assignment table is embedded as an <c>IN (...)</c> subquery; paging and ordering happen in the
    /// database. The sorting expression goes through the base class's safe <c>ApplySorting</c> resolver
    /// (no SQL injection surface).
    /// </para>
    /// </summary>
    public Task<List<Company>> GetPagedByAssignedSpecialistAsync(
        int userId,
        int skipCount,
        int maxResultCount,
        string? sorting = null,
        string? searchText = null,
        bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        if (skipCount < 0)
        {
            throw new BusinessException("The number of records to skip cannot be negative.", "Ensa:InvalidPaging");
        }

        if (maxResultCount <= 0)
        {
            throw new BusinessException("The page size must be greater than zero.", "Ensa:InvalidPaging");
        }

        var assignedCompanyIds = Context.Set<AssignedSpecialist>()
            .AsNoTracking()
            .Where(fi => fi.UserId == userId && fi.IsActive)
            .Select(fi => fi.CompanyId);

        var query = GetReadOnlyQueryable().Where(f => assignedCompanyIds.Contains(f.Id));

        if (onlyActive)
        {
            query = query.Where(f => f.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.Trim();
            query = query.Where(f =>
                f.CompanyName.Contains(search)
                || (f.SsiNumber != null && f.SsiNumber.Contains(search))
                || (f.TaxNumber != null && f.TaxNumber.Contains(search)));
        }

        query = ApplySorting(query, sorting);

        return query.Skip(skipCount).Take(maxResultCount).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> SsiNumberExistsAsync(
        string ssiNumber,
        int? exceptCompanyId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ssiNumber))
        {
            return Task.FromResult(false);
        }

        return GetReadOnlyQueryable()
            .AnyAsync(
                f => f.SsiNumber == ssiNumber && (exceptCompanyId == null || f.Id != exceptCompanyId),
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> GetActiveCompanyCountAsync(CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable().CountAsync(f => f.IsActive && !f.IsOrganizationRecord, cancellationToken);

    /// <summary>
    /// Checks whether the headquarters chain would form a cycle.
    /// <para>
    /// <b>N+1 prevention:</b> the chain is NOT scanned by issuing a query per step; the
    /// <c>(Id, HeadquarterCompanyId)</c> pairs of the current tenant are read in a SINGLE query and walked
    /// in memory. The projection is limited to two <c>int</c> columns, which keeps the cost low.
    /// </para>
    /// </summary>
    public async Task<bool> HasCircularHeadquarterChainAsync(
        int companyId,
        int candidateHeadquarterCompanyId,
        CancellationToken cancellationToken = default)
    {
        // A company cannot be its own headquarters.
        if (companyId == candidateHeadquarterCompanyId)
        {
            return true;
        }

        var chain = await GetReadOnlyQueryable()
            .Where(f => f.HeadquarterCompanyId != null)
            .Select(f => new { f.Id, f.HeadquarterCompanyId })
            .ToDictionaryAsync(x => x.Id, x => x.HeadquarterCompanyId!.Value, cancellationToken);

        // Walk up from the candidate: reaching companyId means the chain forms a cycle.
        var visited = new HashSet<int> { candidateHeadquarterCompanyId };
        var current = candidateHeadquarterCompanyId;

        while (chain.TryGetValue(current, out var parent))
        {
            if (parent == companyId)
            {
                return true;
            }

            // Do not loop forever if the data already contains a cycle.
            if (!visited.Add(parent))
            {
                return true;
            }

            current = parent;
        }

        return false;
    }
}
