using Ensa.Domain.Common;
using Ensa.Domain.Membership;
using Ensa.Domain.Menus;
using Ensa.Domain.Menus.Navigations;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Menus;

/// <summary>
/// Queries specific to the <see cref="Menu"/> module — the repository counterpart of the legacy
/// <c>MenuOperations</c> design.
/// <para>
/// <see cref="Menu"/>, <see cref="MenuNode"/>, <see cref="MenuItem"/>,
/// <see cref="MenuElement"/>, <see cref="MenuType"/> and <see cref="MenuPage"/> are host catalogue tables
/// (they do not implement <c>IMultiTenant</c>). <see cref="UserMenuOverride"/> and
/// <see cref="CompanyModule"/>, by contrast, belong to a tenant; the global query filter takes care of them,
/// so no <c>TenantId</c> predicate is written in these queries.
/// </para>
/// <para>
/// <b>N+1 prevention:</b> the menu tree is NOT built by issuing a query per node; every
/// <see cref="MenuNode"/> + <see cref="MenuItem"/> row of the menu is read in a SINGLE query and turned into
/// a tree in memory. The user customisation and the company module list are one bulk query each as well.
/// </para>
/// </summary>
public class MenuRepository(EnsaDbContext context, IDataFilter dataFilter)
    : EfCoreRepository<Menu>(context, dataFilter), IMenuRepository
{
    /// <summary>User context used while building the menu (collected in a single query round).</summary>
    private sealed record UserContext(
        int UserId,
        string? UserTypeCode,
        int? OrganizationTypeId,
        int? SubscriptionPlanId,
        int? CompanyId);

    /// <summary>Raw row of the menu tree: layout node plus catalogue item.</summary>
    private sealed record MenuLine(MenuNode Detail, MenuItem Item);

    /// <inheritdoc />
    public async Task<MenuNavigation?> GetUserMenuOverrideAsync(
        int userId,
        string menuTypeCode,
        CancellationToken ct = default)
    {
        var userContext = await GetUserContextAsync(userId, ct);
        if (userContext is null)
        {
            return null;
        }

        var menu = await SelectMenuAsync(menuTypeCode, userContext, ct);
        if (menu is null)
        {
            return null;
        }

        return await BuildMenuNavigationAsync(menu, userContext, ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Administration screen view: the module filter and the user customisation are NOT applied, only the
    /// active rows are returned.
    /// </remarks>
    public async Task<MenuNavigation?> GetWithNavigationAsync(int menuId, CancellationToken ct = default)
    {
        var menu = await GetReadOnlyQueryable().FirstOrDefaultAsync(m => m.Id == menuId, ct);
        if (menu is null)
        {
            return null;
        }

        return await BuildMenuNavigationAsync(menu, userContext: null, ct);
    }

    /// <summary>
    /// Finds the menu mapped to a page address through <see cref="MenuPage"/>.
    /// <para>
    /// <b>ASSUMPTION:</b> <see cref="Menu"/> has no separate "code" field; the legacy
    /// <c>MenuPage_T.MenuCode</c> value corresponds to the menu's layout type code
    /// (<see cref="Menu.MenuTypeCode"/>). The mapping is done through that field.
    /// </para>
    /// </summary>
    public async Task<Menu?> FindByPageUrlAsync(
        string pageUrl,
        string? settlementCode = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(pageUrl))
        {
            return null;
        }

        var pageQuery = Context.Set<MenuPage>()
            .AsNoTracking()
            .Where(s => s.PageUrl == pageUrl && s.IsActive);

        if (!string.IsNullOrWhiteSpace(settlementCode))
        {
            pageQuery = pageQuery.Where(s => s.SettlementCode == settlementCode);
        }

        var menuCode = await pageQuery
            .OrderBy(s => s.Id)
            .Select(s => s.MenuCode)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(menuCode))
        {
            return null;
        }

        return await GetReadOnlyQueryable()
            .Where(m => m.IsActive && m.MenuTypeCode == menuCode)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>All</b> menus matching the user are scanned (every layout type). The lines are read in a SINGLE
    /// query for the whole menu id set rather than per menu; the same visibility rules (module filter,
    /// user customisation, parent node pruning) are then applied in memory.
    /// </remarks>
    public async Task<List<string>> GetUserMenuItemCodesAsync(
        int userId,
        CancellationToken ct = default)
    {
        var userContext = await GetUserContextAsync(userId, ct);
        if (userContext is null)
        {
            return [];
        }

        var menuIds = await SuitableMenuQuery(menuTypeCode: null, userContext)
            .Select(m => m.Id)
            .ToListAsync(ct);

        if (menuIds.Count == 0)
        {
            return [];
        }

        var lines = await GetMenuRowsAsync(menuIds, ct);
        var visibleNodes = await CalculateVisibleLinesAsync(lines, userContext, ct);

        return visibleNodes
            .Select(s => s.Item.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();
    }

    /// <inheritdoc />
    public Task<List<int>> GetCompanyModuleIdsAsync(int companyId, CancellationToken ct = default)
        => Context.Set<CompanyModule>()
                  .AsNoTracking()
                  .Where(fm => fm.CompanyId == companyId && fm.IsActive)
                  .Select(fm => fm.ModuleId)
                  .Distinct()
                  .ToListAsync(ct);

    // ------------------------------------------------------------------
    // User context and menu selection
    // ------------------------------------------------------------------

    /// <summary>
    /// Collects the user context used for menu selection: user type code, the organization's type and
    /// subscription plan, and the user's company.
    /// </summary>
    private async Task<UserContext?> GetUserContextAsync(int userId, CancellationToken ct)
    {
        var user = await Context.Set<User>()
            .AsNoTracking()
            .Where(k => k.Id == userId && k.IsActive)
            .Select(k => new { k.Id, k.StaffRole, k.TenantId, k.CompanyId })
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            return null;
        }

        string? userRoleCode = null;
        if (user.StaffRole != StaffRole.Unspecified)
        {
            userRoleCode = await Context.Set<UserType>()
                .AsNoTracking()
                .Where(kt => kt.StaffRole == user.StaffRole && kt.IsActive)
                .OrderBy(kt => kt.SortOrder)
                .Select(kt => kt.Code)
                .FirstOrDefaultAsync(ct);
        }

        int? organizationTypeId = null;
        int? subscriptionPlanId = null;

        if (user.TenantId is int organizationId)
        {
            // Organization is a host table; no tenant filter is applied.
            var organization = await Context.Set<Organization>()
                .AsNoTracking()
                .Where(k => k.Id == organizationId)
                .Select(k => new { k.OrganizationTypeId, k.SubscriptionPlanId })
                .FirstOrDefaultAsync(ct);

            organizationTypeId = organization?.OrganizationTypeId;
            subscriptionPlanId = organization?.SubscriptionPlanId;
        }

        return new UserContext(user.Id, userRoleCode, organizationTypeId, subscriptionPlanId, user.CompanyId);
    }

    /// <summary>
    /// Query that filters the active menus matching the user.
    /// <para>
    /// A <c>null</c> type/organization/plan field on the menu means "applies to all"; when populated it
    /// must match the user's value exactly.
    /// </para>
    /// </summary>
    private IQueryable<Menu> SuitableMenuQuery(string? menuTypeCode, UserContext userContext)
    {
        var query = GetReadOnlyQueryable().Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(menuTypeCode))
        {
            query = query.Where(m => m.MenuTypeCode == menuTypeCode);
        }

        var userRoleCode = userContext.UserTypeCode;
        var organizationTypeId = userContext.OrganizationTypeId;
        var subscriptionPlanId = userContext.SubscriptionPlanId;

        return query.Where(m =>
            (m.UserTypeCode == null || m.UserTypeCode == userRoleCode)
            && (m.OrganizationTypeId == null || m.OrganizationTypeId == organizationTypeId)
            && (m.SubscriptionPlanId == null || m.SubscriptionPlanId == subscriptionPlanId));
    }

    /// <summary>
    /// Selects the menu that best matches the user for the given layout type.
    /// <para>
    /// When more than one menu matches, the <b>most specific</b> one wins: a populated user type is
    /// preferred first, then organization type, then subscription plan; ties are broken by
    /// <c>SortOrder</c> and <c>Id</c>.
    /// </para>
    /// </summary>
    private Task<Menu?> SelectMenuAsync(string menuTypeCode, UserContext userContext, CancellationToken ct)
        => SuitableMenuQuery(menuTypeCode, userContext)
            .OrderBy(m => m.UserTypeCode == null ? 1 : 0)
            .ThenBy(m => m.OrganizationTypeId == null ? 1 : 0)
            .ThenBy(m => m.SubscriptionPlanId == null ? 1 : 0)
            .ThenBy(m => m.SortOrder)
            .ThenBy(m => m.Id)
            .FirstOrDefaultAsync(ct);

    // ------------------------------------------------------------------
    // Tree building
    // ------------------------------------------------------------------

    /// <summary>
    /// Fills in the menu root, the layout type, the <see cref="MenuNode"/> tree and the free-form
    /// <see cref="MenuElement"/> tree.
    /// </summary>
    /// <param name="userContext">
    /// When <c>null</c>, the user-specific filters (module + customisation) are not applied —
    /// the menu administration screen view.
    /// </param>
    private async Task<MenuNavigation> BuildMenuNavigationAsync(
        Menu menu,
        UserContext? userContext,
        CancellationToken ct)
    {
        var navigation = new MenuNavigation { Menu = menu };

        if (!string.IsNullOrWhiteSpace(menu.MenuTypeCode))
        {
            navigation.MenuType = await Context.Set<MenuType>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Code == menu.MenuTypeCode, ct);
        }

        var lines = await GetMenuRowsAsync([menu.Id], ct);

        HashSet<int> userRemovedItemIds = [];
        if (userContext is not null)
        {
            userRemovedItemIds = await GetRemovedItemIdsAsync(userContext.UserId, ct);
            lines = await CalculateVisibleLinesAsync(lines, userContext, ct);
        }

        navigation.Roots = BuildNodeTree(lines, userRemovedItemIds);
        navigation.ElementRoots = await BuildElementTreeAsync(menu.Id, ct);

        return navigation;
    }

    /// <summary>
    /// Reads the active layout and catalogue rows for a set of menu ids <b>in a single query</b>.
    /// </summary>
    private async Task<List<MenuLine>> GetMenuRowsAsync(List<int> menuIds, CancellationToken ct)
    {
        // The projection targets an anonymous type (the shape EF translates reliably); the
        // conversion to the record type is completed in memory. One single round trip.
        var rows = await (from detail in Context.Set<MenuNode>().AsNoTracking()
                         join item in Context.Set<MenuItem>().AsNoTracking()
                             on detail.MenuItemId equals item.Id
                         where menuIds.Contains(detail.MenuId) && detail.IsActive && item.IsActive
                         select new { Detail = detail, Item = item })
                        .ToListAsync(ct);

        return [.. rows.Select(x => new MenuLine(x.Detail, x.Item))];
    }

    /// <summary>
    /// Applies the user-specific visibility rules:
    /// <list type="number">
    ///   <item><b>Module filter:</b> items with a populated <see cref="MenuItem.ModuleId"/> are visible only
    ///         if that module is enabled for the user's company.</item>
    ///   <item><b>User customisation:</b> <c>Added</c> rows are visible even if the module filter would
    ///         exclude them; <c>Removed</c> rows drop out under all circumstances.</item>
    ///   <item><b>Pruning:</b> child nodes of a removed parent are removed too — no orphan nodes remain.</item>
    /// </list>
    /// The module list and the customisation rows are one bulk query each; there is no query per item.
    /// </summary>
    private async Task<List<MenuLine>> CalculateVisibleLinesAsync(
        List<MenuLine> lines,
        UserContext userContext,
        CancellationToken ct)
    {
        if (lines.Count == 0)
        {
            return lines;
        }

        HashSet<int> companyModuleIds = [];
        if (userContext.CompanyId is int companyId)
        {
            companyModuleIds = (await GetCompanyModuleIdsAsync(companyId, ct)).ToHashSet();
        }

        var customizations = await Context.Set<UserMenuOverride>()
            .AsNoTracking()
            .Where(km => km.UserId == userContext.UserId)
            .Select(km => new { km.MenuItemId, km.Operation })
            .ToListAsync(ct);

        var addedItemIds = customizations
            .Where(x => x.Operation == UserMenuOverrideAction.Added)
            .Select(x => x.MenuItemId)
            .ToHashSet();

        var removedItemIds = customizations
            .Where(x => x.Operation == UserMenuOverrideAction.Removed)
            .Select(x => x.MenuItemId)
            .ToHashSet();

        bool DirectVisible(MenuLine line)
        {
            if (removedItemIds.Contains(line.Item.Id))
            {
                return false;
            }

            if (line.Item.ModuleId is not int moduleId)
            {
                return true;
            }

            return companyModuleIds.Contains(moduleId) || addedItemIds.Contains(line.Item.Id);
        }

        var allLines = lines.ToDictionary(s => s.Detail.Id);
        var directlyVisibleNodes = lines.Where(DirectVisible).Select(s => s.Detail.Id).ToHashSet();

        // Pruning: a node is visible only if ALL of its ancestors are visible too.
        var decision = new Dictionary<int, bool>();

        bool Visible(int detailId)
        {
            if (decision.TryGetValue(detailId, out var previous))
            {
                return previous;
            }

            // Guard against cyclic data: treat the node as "invisible" while it is being computed.
            decision[detailId] = false;

            if (!allLines.TryGetValue(detailId, out var line) || !directlyVisibleNodes.Contains(detailId))
            {
                return false;
            }

            var result = line.Detail.ParentMenuNodeId is not int parentId || Visible(parentId);

            decision[detailId] = result;
            return result;
        }

        return lines.Where(s => Visible(s.Detail.Id)).ToList();
    }

    /// <summary>Ids of the items hidden by the user — for highlighting them on the editing screen.</summary>
    private async Task<HashSet<int>> GetRemovedItemIdsAsync(int userId, CancellationToken ct)
    {
        var ids = await Context.Set<UserMenuOverride>()
            .AsNoTracking()
            .Where(km => km.UserId == userId && km.Operation == UserMenuOverrideAction.Removed)
            .Select(km => km.MenuItemId)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    /// <summary>Turns a flat list of rows into a hierarchical tree in memory (no extra query).</summary>
    private static List<MenuNodeNavigation> BuildNodeTree(
        List<MenuLine> lines,
        HashSet<int> userRemovedItemIds)
    {
        var nodes = lines.ToDictionary(
            s => s.Detail.Id,
            s => new MenuNodeNavigation
            {
                MenuNode = s.Detail,
                MenuItem = s.Item,
                HiddenForUser = userRemovedItemIds.Contains(s.Item.Id)
            });

        var roots = new List<MenuNodeNavigation>();

        foreach (var line in lines)
        {
            var node = nodes[line.Detail.Id];

            if (line.Detail.ParentMenuNodeId is int parentId
                && nodes.TryGetValue(parentId, out var parent))
            {
                parent.ChildNodes.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        foreach (var node in nodes.Values)
        {
            SortDetail(node.ChildNodes);
        }

        SortDetail(roots);
        return roots;
    }

    /// <summary>Builds the free-form menu element tree in a SINGLE query.</summary>
    private async Task<List<MenuElementNavigation>> BuildElementTreeAsync(int menuId, CancellationToken ct)
    {
        var elements = await Context.Set<MenuElement>()
            .AsNoTracking()
            .Where(e => e.MenuId == menuId && e.IsActive)
            .ToListAsync(ct);

        if (elements.Count == 0)
        {
            return [];
        }

        var nodes = elements.ToDictionary(e => e.Id, e => new MenuElementNavigation { MenuElement = e });
        var roots = new List<MenuElementNavigation>();

        foreach (var element in elements)
        {
            var node = nodes[element.Id];

            if (element.ParentMenuElementId is int parentId && nodes.TryGetValue(parentId, out var parent))
            {
                parent.ChildNodes.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        foreach (var node in nodes.Values)
        {
            SortElement(node.ChildNodes);
        }

        SortElement(roots);
        return roots;
    }

    /// <summary>Orders by <c>SortOrder</c> at every level, with <c>Id</c> as a tie-breaker.</summary>
    private static void SortDetail(List<MenuNodeNavigation> nodes)
    {
        if (nodes.Count > 1)
        {
            nodes.Sort((left, right) =>
            {
                var comparison = left.MenuNode.SortOrder.CompareTo(right.MenuNode.SortOrder);
                return comparison != 0 ? comparison : left.MenuNode.Id.CompareTo(right.MenuNode.Id);
            });
        }
    }

    /// <inheritdoc cref="SortDetail" />
    private static void SortElement(List<MenuElementNavigation> nodes)
    {
        if (nodes.Count > 1)
        {
            nodes.Sort((left, right) =>
            {
                var comparison = left.MenuElement.SortOrder.CompareTo(right.MenuElement.SortOrder);
                return comparison != 0 ? comparison : left.MenuElement.Id.CompareTo(right.MenuElement.Id);
            });
        }
    }
}
