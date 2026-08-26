using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Companies.Navigations;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Companies;

/// <summary>
/// Queries specific to the <see cref="WorkplaceDepartment"/> module.
/// <para>
/// Every entity is tenant and soft-delete filtered; the <c>TenantId</c> / <c>IsDeleted</c> predicate is never
/// written by hand in these queries.
/// </para>
/// </summary>
public class WorkplaceDepartmentRepository(EnsaDbContext context, IDataFilter dataFilter)
    : EfCoreRepository<WorkplaceDepartment>(context, dataFilter), IWorkplaceDepartmentRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// The documents and the employees are each fetched with one bulk query; there is NO query per row.
    /// </remarks>
    public async Task<WorkplaceDepartmentNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var department = await GetReadOnlyQueryable().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (department is null)
        {
            return null;
        }

        var navigation = new WorkplaceDepartmentNavigation { WorkplaceDepartment = department };

        var company = await Context.Set<Company>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == department.CompanyId, cancellationToken);

        if (company is not null)
        {
            navigation.Company = company;
        }

        navigation.Documents = await Context.Set<DepartmentDocument>()
            .AsNoTracking()
            .Where(e => e.WorkplaceDepartmentId == id)
            .OrderByDescending(e => e.ExaminationDate)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);

        navigation.Employees = await Context.Set<CompanyEmployee>()
            .AsNoTracking()
            .Where(p => p.AssignedDepartmentId == id)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.LastName)
            .ToListAsync(cancellationToken);

        return navigation;
    }

    /// <inheritdoc />
    public Task<List<WorkplaceDepartment>> GetListByCompanyAsync(
        int companyId,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.DepartmentName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> DepartmentNameExistsAsync(
        string departmentName,
        int companyId,
        int? exceptId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
        {
            return Task.FromResult(false);
        }

        var name = departmentName.Trim();

        return GetReadOnlyQueryable()
            .AnyAsync(
                b => b.CompanyId == companyId
                     && b.DepartmentName == name
                     && (exceptId == null || b.Id != exceptId),
                cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Because this is a pre-delete check, inactive employees are counted too: the department cannot be
    /// deleted while any record is attached to it.
    /// </remarks>
    public Task<int> GetEmployeeCountAsync(
        int workplaceDepartmentId,
        CancellationToken cancellationToken = default)
        => Context.Set<CompanyEmployee>()
                  .AsNoTracking()
                  .CountAsync(p => p.AssignedDepartmentId == workplaceDepartmentId, cancellationToken);
}
