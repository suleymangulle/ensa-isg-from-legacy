using Ensa.Domain.Companies.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Companies;

/// <summary>
/// Module-specific queries for <see cref="CompanyEmployee"/>.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface ICompanyEmployeeRepository : IRepository<CompanyEmployee>
{
    /// <summary>
    /// Loads the employee in their combined form (company, department, health information,
    /// immunizations, family history, work history, duties and the latest training dates).
    /// </summary>
    Task<CompanyEmployeeNavigation?> GetWithNavigationAsync(
        int id,
        bool includeHealthInfo = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports whether the given national ID is already in use at this company.
    /// In update scenarios the record itself is excluded through <paramref name="exceptId"/>.
    /// </summary>
    Task<bool> NationalIdExistsAsync(
        string nationalId,
        int companyId,
        int? exceptId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the <b>active</b> employee records with the given national ID across the whole
    /// active tenant, regardless of company. Used by the "the same person cannot be active at
    /// more than one workplace" rule.
    /// </summary>
    Task<List<CompanyEmployee>> GetActiveRecordsByNationalIdAsync(
        string nationalId,
        int? exceptId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Number of active (still employed) employees at the company.</summary>
    Task<int> GetActiveEmployeeCountAsync(
        int companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the latest attendance date per training for the employees of a company.
    /// It is produced by projection over <see cref="CompanyEmployeeDocument"/>, grouped by
    /// <c>TrainingId</c> and taking the maximum <c>DocumentDate</c>; there is no backing table.
    /// </summary>
    Task<List<EmployeeLatestTrainingInfo>> GetLatestTrainingDatesAsync(
        int companyId,
        CancellationToken cancellationToken = default);
}
