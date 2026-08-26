using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Companies.Navigations;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Companies;

/// <summary>
/// Queries specific to the <see cref="CompanyEmployee"/> module.
/// <para>
/// Every entity is tenant and soft-delete filtered; the <c>TenantId</c> / <c>IsDeleted</c> predicate is never
/// written by hand in these queries.
/// </para>
/// </summary>
public class CompanyEmployeeRepository(EnsaDbContext context, IDataFilter dataFilter)
    : EfCoreRepository<CompanyEmployee>(context, dataFilter), ICompanyEmployeeRepository
{
    /// <summary>
    /// Returns the employee in its combined representation.
    /// <para>
    /// Each child collection is fetched with a SINGLE query; the total query count is constant
    /// (independent of the number of records). When <paramref name="includeHealthInfo"/> is false, no query
    /// is issued for the health child records at all.
    /// </para>
    /// </summary>
    public async Task<CompanyEmployeeNavigation?> GetWithNavigationAsync(
        int id,
        bool includeHealthInfo = true,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetReadOnlyQueryable().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        var navigation = new CompanyEmployeeNavigation { CompanyEmployee = employee };

        var company = await Context.Set<Company>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == employee.CompanyId, cancellationToken);

        if (company is not null)
        {
            navigation.Company = company;
        }

        if (employee.AssignedDepartmentId is int departmentId)
        {
            navigation.AssignedDepartment = await Context.Set<WorkplaceDepartment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == departmentId, cancellationToken);
        }

        if (includeHealthInfo)
        {
            navigation.HealthInfo = await Context.Set<EmployeeHealthInfo>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CompanyEmployeeId == id, cancellationToken);

            navigation.Immunizations = await Context.Set<EmployeeImmunization>()
                .AsNoTracking()
                .Where(b => b.CompanyEmployeeId == id)
                .OrderByDescending(b => b.Date)
                .ThenBy(b => b.Id)
                .ToListAsync(cancellationToken);

            navigation.FamilyHistory = await Context.Set<EmployeeFamilyHistory>()
                .AsNoTracking()
                .Where(s => s.CompanyEmployeeId == id)
                .OrderBy(s => s.Id)
                .ToListAsync(cancellationToken);
        }

        navigation.WorkHistory = await Context.Set<EmployeeWorkHistory>()
            .AsNoTracking()
            .Where(c => c.CompanyEmployeeId == id)
            .OrderBy(c => c.OrderNo)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

        navigation.Duties = await Context.Set<CompanyEmployeeDuty>()
            .AsNoTracking()
            .Where(g => g.CompanyEmployeeId == id && g.IsActive)
            .OrderBy(g => g.DutyId)
            .ToListAsync(cancellationToken);

        navigation.LatestTrainings = await CalculateLatestTrainingsAsync(
            companyId: null,
            companyEmployeeId: id,
            cancellationToken);

        return navigation;
    }

    /// <inheritdoc />
    public Task<bool> NationalIdExistsAsync(
        string nationalId,
        int companyId,
        int? exceptId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
        {
            return Task.FromResult(false);
        }

        return GetReadOnlyQueryable()
            .AnyAsync(
                p => p.NationalId == nationalId
                     && p.CompanyId == companyId
                     && (exceptId == null || p.Id != exceptId),
                cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// "Active" means the record is active (<c>IsActive</c>) and has no termination date.
    /// Companies are not distinguished; every workplace in the current tenant is scanned.
    /// </remarks>
    public Task<List<CompanyEmployee>> GetActiveRecordsByNationalIdAsync(
        string nationalId,
        int? exceptId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
        {
            return Task.FromResult(new List<CompanyEmployee>());
        }

        return GetReadOnlyQueryable()
            .Where(p => p.NationalId == nationalId
                        && p.IsActive
                        && p.TerminationDate == null
                        && (exceptId == null || p.Id != exceptId))
            .OrderBy(p => p.CompanyId)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> GetActiveEmployeeCountAsync(int companyId, CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .CountAsync(
                p => p.CompanyId == companyId && p.IsActive && p.TerminationDate == null,
                cancellationToken);

    /// <inheritdoc />
    public Task<List<EmployeeLatestTrainingInfo>> GetLatestTrainingDatesAsync(
        int companyId,
        CancellationToken cancellationToken = default)
        => CalculateLatestTrainingsAsync(companyId, companyEmployeeId: null, cancellationToken);

    // ------------------------------------------------------------------

    /// <summary>
    /// Produces the most recent attendance record per employee × training.
    /// <para>
    /// <b>Why a single query plus in-memory grouping?</b> If only <c>MAX(DocumentDate)</c> were needed the
    /// query could have been grouped entirely in the database; but the result must also contain the
    /// <c>Id</c> of the <see cref="CompanyEmployeeDocument"/> record that supplied that date. Solving this in
    /// SQL would require a second self-join. Instead the required <b>narrow</b> columns (5 fields) are
    /// fetched in a SINGLE query and grouped in memory — one round trip, no N+1.
    /// </para>
    /// </summary>
    private async Task<List<EmployeeLatestTrainingInfo>> CalculateLatestTrainingsAsync(
        int? companyId,
        int? companyEmployeeId,
        CancellationToken cancellationToken)
    {
        var query = from document in Context.Set<CompanyEmployeeDocument>().AsNoTracking()
                    join employee in GetReadOnlyQueryable() on document.CompanyEmployeeId equals employee.Id
                    where document.IsActive && document.TrainingId != null
                    select new
                    {
                        DocumentId = document.Id,
                        document.CompanyEmployeeId,
                        document.TrainingId,
                        document.DocumentDate,
                        employee.Name,
                        employee.LastName,
                        employee.CompanyId
                    };

        if (companyId is int company)
        {
            query = query.Where(x => x.CompanyId == company);
        }

        if (companyEmployeeId is int employeeId)
        {
            query = query.Where(x => x.CompanyEmployeeId == employeeId);
        }

        var lines = await query.ToListAsync(cancellationToken);

        return [.. lines
            .GroupBy(x => new { x.CompanyEmployeeId, x.TrainingId })
            .Select(group =>
            {
                var enCurrent = group
                    .OrderByDescending(x => x.DocumentDate)
                    .ThenByDescending(x => x.DocumentId)
                    .First();

                return new EmployeeLatestTrainingInfo
                {
                    CompanyEmployeeId = enCurrent.CompanyEmployeeId,
                    Name = enCurrent.Name,
                    LastName = enCurrent.LastName,
                    TrainingId = enCurrent.TrainingId,
                    TrainingDate = enCurrent.DocumentDate,
                    CompanyEmployeeDocumentId = enCurrent.DocumentId
                };
            })
            .OrderBy(x => x.CompanyEmployeeId)
            .ThenBy(x => x.TrainingId)];
    }
}
