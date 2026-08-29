using Ensa.Domain.Menus.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Menus;

/// <summary>
/// Module-specific repository contract for <see cref="Menu"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface IMenuRepository : IRepository<Menu>
{
    /// <summary>
    /// Builds the render-ready menu tree for a user.
    /// <para>The filters below are applied in the same order as the legacy <c>MenuOperations</c>
    /// routine:</para>
    /// <list type="number">
    ///   <item>Menu selection: the active menu matching <paramref name="menuTypeCode"/>, the user's
    ///         type, and the organization's <c>OrganizationTypeId</c>/<c>SubscriptionPlanId</c>.</item>
    ///   <item>Active <see cref="MenuNode"/> and <see cref="MenuItem"/> rows only.</item>
    ///   <item>Permission filter: items with a <see cref="MenuItem.PermissionId"/> are shown only
    ///         when <paramref name="effectivePermissionIds"/> contains it. An item that names no
    ///         permission is not governed and is always shown.</item>
    ///   <item>Module filter: items with a <see cref="MenuItem.ModuleId"/> are shown only when that
    ///         module is enabled for the company through <see cref="CompanyModule"/>.</item>
    ///   <item>User customisation: <see cref="UserMenuOverride"/> — <c>Removed</c> entries are
    ///         dropped, <c>Added</c> entries are included even if the module filter excludes them.</item>
    ///   <item>Ordering: <c>SortOrder</c> at every level.</item>
    /// </list>
    /// Returns <c>null</c> when no menu matches.
    /// </summary>
    /// <param name="effectivePermissionIds">
    /// The caller's effective permissions, resolved by <c>IPermissionManager</c> in the application
    /// layer. It is passed in rather than looked up here so the one implementation of the legacy
    /// four-gate algorithm stays in the domain service and a repository does not depend on it.
    /// Pass <c>null</c> to skip the permission filter - the administration view does, because it
    /// shows the menu as configured rather than as one user sees it.
    /// </param>
    Task<MenuNavigation?> GetUserMenuOverrideAsync(
        int userId,
        string menuTypeCode,
        IReadOnlySet<int>? effectivePermissionIds,
        CancellationToken ct = default);

    /// <summary>Loads the menu with its whole tree, for the menu ADMIN screen — no user filter is applied.</summary>
    Task<MenuNavigation?> GetWithNavigationAsync(int menuId, CancellationToken ct = default);

    /// <summary>
    /// Finds the menu mapped to a page URL through <see cref="MenuPage"/>.
    /// </summary>
    Task<Menu?> FindByPageUrlAsync(string pageUrl, string? settlementCode = null, CancellationToken ct = default);

    /// <summary>
    /// Every <see cref="MenuItem"/> code visible in the user's menu, for client-side route guarding
    /// and quick permission checks.
    /// </summary>
    Task<List<string>> GetUserMenuItemCodesAsync(
        int userId,
        IReadOnlySet<int>? effectivePermissionIds,
        CancellationToken ct = default);

    /// <summary>Ids of the active modules enabled for a company; this feeds the menu module filter.</summary>
    Task<List<int>> GetCompanyModuleIdsAsync(int companyId, CancellationToken ct = default);
}
