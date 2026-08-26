using Ensa.Domain.Repositories;
using Ensa.Domain.Tenancy.Navigations;

namespace Ensa.Domain.Tenancy;

/// <summary>
/// Module-specific repository contract for <see cref="Organization"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface IOrganizationRepository : IRepository<Organization>
{
    /// <summary>Resolves a tenant by its unique organization code, for sign-in and sub-domain routing.</summary>
    Task<Organization?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Whether the organization code is already used by another record. (<paramref name="exceptOrganizationId"/> is excluded, for the update case.)</summary>
    Task<bool> CodeExistsAsync(string code, int? exceptOrganizationId = null, CancellationToken cancellationToken = default);

    /// <summary>Loads the organization with its type, plan, offices and contract in a single query.</summary>
    Task<OrganizationNavigation?> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The number of active — not soft-deleted — users in the organization, for subscription quota checks.</summary>
    Task<int> GetActiveUserCountAsync(int organizationId, CancellationToken cancellationToken = default);

    /// <summary>Organizations that are still active although their subscription expired as of <paramref name="date"/>.</summary>
    Task<List<Organization>> GetDurationExpiredAsync(DateTime date, CancellationToken cancellationToken = default);
}
