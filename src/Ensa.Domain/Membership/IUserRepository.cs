using Ensa.Domain.Membership.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Membership;

/// <summary>
/// Module-specific repository contract for <see cref="User"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Returns the user's EFFECTIVE permissions: user permissions plus user type permissions,
    /// passed through the organization type and subscription plan gates and with explicit deny
    /// rows removed. See <see cref="IPermissionManager"/> for the calculation rule.
    /// </summary>
    /// <summary>
    /// The facts authorization needs, gathered in one query across the account, the profile, the
    /// employment and the role assignments. Returns <c>null</c> when there is no such user.
    /// </summary>
    Task<UserAuthorizationFacts?> GetAuthorizationFactsAsync(int userId, CancellationToken ct = default);


    /// <summary>Loads the user together with organization, office, role and permission data in a single call.</summary>
    Task<UserNavigation?> GetWithNavigationAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Determines whether the national ID is already registered to another user in the active
    /// tenant. <paramref name="exceptUserId"/> excludes the record itself in update scenarios.
    /// </summary>
    Task<bool> NationalIdExistsAsync(string nationalId, int? exceptUserId = null, CancellationToken ct = default);

    /// <summary>Looks a user up by normalised user name (with the tenant filter applied).</summary>
    Task<User?> FindByUserNameAsync(string userName, CancellationToken ct = default);

    /// <summary>Active users assigned to the given office (through <see cref="UserOffice"/>).</summary>
    Task<List<User>> GetByOfficeAsync(int officeId, CancellationToken ct = default);

    /// <summary>Active users holding at least one of the given roles.</summary>
    Task<List<User>> GetByRolesAsync(IEnumerable<string> roleNames, CancellationToken ct = default);

    /// <summary>The user's role names — used when building token claims.</summary>
    Task<List<string>> GetRoleNamesAsync(int userId, CancellationToken ct = default);
}
