using Ensa.Domain.Companies.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Companies;

/// <summary>
/// Module-specific queries for <see cref="Company"/>.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface ICompanyRepository : IRepository<Company>
{
    /// <summary>
    /// Loads the company in its combined form (province/district/neighbourhood lookups,
    /// employees, assigned specialists, departments, headquarter/branches and the compliance
    /// summary).
    /// </summary>
    /// <param name="id">Company id.</param>
    /// <param name="includeEmployees">Whether to populate the employee list (expensive for large companies).</param>
    /// <param name="includeBranches">Whether to populate the branch list.</param>
    Task<CompanyNavigation?> GetWithNavigationAsync(
        int id,
        bool includeEmployees = true,
        bool includeBranches = true,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the branches attached to a headquarter company.</summary>
    Task<List<Company>> GetBranchesAsync(
        int headquarterCompanyId,
        bool onlyActive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of the companies the given user (specialist/physician) is assigned to.
    /// The filter runs through <see cref="AssignedSpecialist"/>.
    /// </summary>
    Task<List<Company>> GetPagedByAssignedSpecialistAsync(
        int userId,
        int skipCount,
        int maxResultCount,
        string? sorting = null,
        string? searchText = null,
        bool onlyActive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports whether the given SSI workplace registration number is already in use within the
    /// active tenant. In update scenarios the record itself is excluded through
    /// <paramref name="exceptCompanyId"/>.
    /// </summary>
    Task<bool> SsiNumberExistsAsync(
        string ssiNumber,
        int? exceptCompanyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Number of active client workplaces in the active tenant.
    /// The organization's own record (<see cref="Company.IsOrganizationRecord"/>) is excluded
    /// from the count.
    /// </summary>
    Task<int> GetActiveCompanyCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the headquarter chain would form a cycle: returns <c>true</c> when
    /// <paramref name="candidateHeadquarterCompanyId"/> sits inside the branch tree of
    /// <paramref name="companyId"/>.
    /// </summary>
    Task<bool> HasCircularHeadquarterChainAsync(
        int companyId,
        int candidateHeadquarterCompanyId,
        CancellationToken cancellationToken = default);
}
