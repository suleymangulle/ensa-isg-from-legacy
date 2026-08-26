using Ensa.Domain.Companies.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Companies;

/// <summary>
/// Module-specific queries for <see cref="WorkplaceDepartment"/>.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IWorkplaceDepartmentRepository : IRepository<WorkplaceDepartment>
{
    /// <summary>Loads the department together with its documents and the employees working in it.</summary>
    Task<WorkplaceDepartmentNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>Loads a company's departments ordered by name.</summary>
    Task<List<WorkplaceDepartment>> GetListByCompanyAsync(
        int companyId,
        CancellationToken cancellationToken = default);

    /// <summary>Reports whether a department with the same name already exists at the company.</summary>
    Task<bool> DepartmentNameExistsAsync(
        string departmentName,
        int companyId,
        int? exceptId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Number of employees registered to the department (for the pre-delete check).</summary>
    Task<int> GetEmployeeCountAsync(
        int workplaceDepartmentId,
        CancellationToken cancellationToken = default);
}
