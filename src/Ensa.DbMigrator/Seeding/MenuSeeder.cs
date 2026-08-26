using Ensa.Domain.Menus;
using Ensa.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DbMigrator.Seeding;

/// <summary>
/// Seeds the main menu from the SPA's own navigation.
/// <para>
/// <b>Why this exists.</b> The menu tables shipped empty. <c>GET api/menu</c> returned nothing, so
/// the menu administration screen listed no rows, and <c>GET api/menu/my-menu</c> answered
/// <i>"No menu is defined for this layout type"</i> to every user — a whole legacy module present
/// in the domain, the API and the interface, and inert in all three.
/// </para>
/// <para>
/// <b>Where the rows come from.</b> <see cref="MenuSeedData"/> is generated from
/// <c>react/ensa-web/src/pages/*/module.tsx</c> by <c>tools/gen-enums/gen_menu.py</c>. Writing
/// the menu out by hand would have created a second navigation definition that drifts from the
/// first within a release; generating it means the two cannot disagree, and
/// <c>tools/api-tests/frontend_menu.py</c> fails if they ever do.
/// </para>
/// <para>
/// <b>What renders what.</b> The SPA sidebar renders from code and does not read these rows —
/// navigation must not wait on a round trip, and the code path also applies the permission filter
/// (ADR-023, ADR-031). These rows are the legacy module's administration surface and the answer
/// <c>my-menu</c> gives, including the per-user <c>UserMenuOverride</c> rows it honours.
/// </para>
/// <para>
/// <b>It refreshes rather than preserves.</b> Unlike the staff-type defaults, the generated fields
/// of an existing row are rewritten on every run: a renamed or moved screen must not keep its old
/// label and dead URL forever. Rows an administrator added themselves are left untouched, and so
/// is anything the generated set does not name.
/// </para>
/// </summary>
public class MenuSeeder(EnsaDbContext context, ILogger<MenuSeeder> logger) : IDataSeeder
{
    /// <summary>Runs after the reference data, which the menu selection rules read.</summary>
    public int Order => 160;

    public string Name => "Main menu (generated from the SPA navigation)";

    /// <summary>Name of the menu row the whole product shares.</summary>
    private const string MainMenuName = "Ensa main menu";

    /// <summary>Prefix that marks a section heading rather than a navigable screen.</summary>
    private const string GroupCodePrefix = "GROUP-";

    /// <summary>Sections are numbered in tens so an entry can be slotted between two of them.</summary>
    private const int GroupSortStep = 100;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var menuTypeId = await EnsureMenuTypeAsync(cancellationToken);
        var menuId = await EnsureMenuAsync(cancellationToken);

        var itemIds = await EnsureMenuItemsAsync(cancellationToken);
        var nodeCount = await EnsureMenuNodesAsync(menuId, itemIds, cancellationToken);

        logger.LogInformation(
            "Main menu ready: menu type {MenuTypeId}, menu {MenuId}, {ItemCount} item(s), "
            + "{NodeCount} node(s) across {GroupCount} section(s).",
            menuTypeId, menuId, itemIds.Count, nodeCount, MenuSeedData.Groups.Length);
    }

    // ------------------------------------------------------------------ menu type

    private async Task<int> EnsureMenuTypeAsync(CancellationToken cancellationToken)
    {
        var menuType = await context.Set<MenuType>()
            .FirstOrDefaultAsync(t => t.Code == MenuSeedData.MainMenuTypeCode, cancellationToken);

        if (menuType is not null)
        {
            return menuType.Id;
        }

        menuType = new MenuType
        {
            Code = MenuSeedData.MainMenuTypeCode,
            Name = "Main navigation",
            IsActive = true,
            SortOrder = 10
        };

        context.Set<MenuType>().Add(menuType);
        await context.SaveChangesAsync(cancellationToken);

        return menuType.Id;
    }

    // ------------------------------------------------------------------ menu

    private async Task<int> EnsureMenuAsync(CancellationToken cancellationToken)
    {
        var menu = await context.Set<Menu>()
            .FirstOrDefaultAsync(
                m => m.MenuTypeCode == MenuSeedData.MainMenuTypeCode && m.Name == MainMenuName,
                cancellationToken);

        if (menu is not null)
        {
            return menu.Id;
        }

        menu = new Menu
        {
            Name = MainMenuName,
            MenuTypeCode = MenuSeedData.MainMenuTypeCode,
            // All three selectors stay null: this menu applies to every staff type, every
            // organization type and every subscription plan. A narrower menu added later wins over
            // it, because MenuRepository prefers the most specific match.
            UserTypeCode = null,
            OrganizationTypeId = null,
            SubscriptionPlanId = null,
            IsActive = true,
            SortOrder = 10
        };

        context.Set<Menu>().Add(menu);
        await context.SaveChangesAsync(cancellationToken);

        return menu.Id;
    }

    // ------------------------------------------------------------------ items

    /// <summary>Creates or refreshes one <see cref="MenuItem"/> per section and per screen.</summary>
    private async Task<Dictionary<string, int>> EnsureMenuItemsAsync(CancellationToken cancellationToken)
    {
        var wanted = new List<(string Code, string Name, string? Url, string? Icon, int SortOrder)>();

        for (var index = 0; index < MenuSeedData.Groups.Length; index++)
        {
            var (group, name) = MenuSeedData.Groups[index];

            // A section heading is not navigable, so it carries no URL.
            wanted.Add((GroupCodePrefix + group.ToUpperInvariant(), name, null, null,
                        (index + 1) * GroupSortStep));
        }

        foreach (var entry in MenuSeedData.Entries)
        {
            wanted.Add((entry.Code, entry.Name, entry.Url, entry.Icon, entry.SortOrder));
        }

        var codes = wanted.ConvertAll(item => item.Code);

        var existing = await context.Set<MenuItem>()
            .Where(item => codes.Contains(item.Code))
            .ToDictionaryAsync(item => item.Code, cancellationToken);

        var inserted = 0;
        var refreshed = 0;

        foreach (var (code, name, url, icon, sortOrder) in wanted)
        {
            if (existing.TryGetValue(code, out var item))
            {
                if (item.Name == name && item.Url == url && item.IconCssClass == icon
                    && item.SortOrder == sortOrder && item.IsActive)
                {
                    continue;
                }

                item.Name = name;
                item.Url = url;
                item.IconCssClass = icon;
                item.SortOrder = sortOrder;
                item.IsActive = true;
                refreshed++;
                continue;
            }

            item = new MenuItem
            {
                Code = code,
                Name = name,
                Url = url,
                IconCssClass = icon,
                SortOrder = sortOrder,
                IsActive = true
            };

            context.Set<MenuItem>().Add(item);
            existing[code] = item;
            inserted++;
        }

        if (inserted > 0 || refreshed > 0)
        {
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Menu items: {Inserted} inserted, {Refreshed} refreshed.", inserted, refreshed);
        }

        return existing.ToDictionary(pair => pair.Key, pair => pair.Value.Id);
    }

    // ------------------------------------------------------------------ nodes

    /// <summary>
    /// Hangs every item on the menu: the section headings at the root, each screen under its
    /// section. A node already present keeps its place — moving a node is an administrator's
    /// decision — but a missing one is created.
    /// </summary>
    private async Task<int> EnsureMenuNodesAsync(
        int menuId,
        Dictionary<string, int> itemIds,
        CancellationToken cancellationToken)
    {
        var nodes = await context.Set<MenuNode>()
            .Where(node => node.MenuId == menuId)
            .ToListAsync(cancellationToken);

        var byItemId = nodes.ToDictionary(node => node.MenuItemId, node => node);

        var groupNodeIds = new Dictionary<string, MenuNode>(StringComparer.Ordinal);
        var inserted = 0;

        for (var index = 0; index < MenuSeedData.Groups.Length; index++)
        {
            var (group, _) = MenuSeedData.Groups[index];
            var itemId = itemIds[GroupCodePrefix + group.ToUpperInvariant()];

            if (!byItemId.TryGetValue(itemId, out var node))
            {
                node = new MenuNode
                {
                    MenuId = menuId,
                    MenuItemId = itemId,
                    ParentMenuNodeId = null,
                    SortOrder = (index + 1) * GroupSortStep,
                    IsActive = true
                };

                context.Set<MenuNode>().Add(node);
                byItemId[itemId] = node;
                inserted++;
            }

            groupNodeIds[group] = node;
        }

        // The section nodes need their identities before anything can point at them.
        if (inserted > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        foreach (var entry in MenuSeedData.Entries)
        {
            var itemId = itemIds[entry.Code];
            if (byItemId.ContainsKey(itemId))
            {
                continue;
            }

            var node = new MenuNode
            {
                MenuId = menuId,
                MenuItemId = itemId,
                ParentMenuNodeId = groupNodeIds[entry.Group].Id,
                SortOrder = entry.SortOrder,
                IsActive = true
            };

            context.Set<MenuNode>().Add(node);
            byItemId[itemId] = node;
            inserted++;
        }

        if (inserted > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Menu nodes: {Inserted} inserted.", inserted);
        }

        return byItemId.Count;
    }
}
