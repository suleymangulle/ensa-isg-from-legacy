using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Menus;
using Ensa.Application.Contracts.Menus.Dtos;
using Ensa.Application.Contracts.Menus.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Membership;
using Ensa.Domain.Menus;
using Ensa.Domain.Menus.Navigations;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Application.Menus;

/// <summary>
/// Menu definitions and the per-user rendered menu.
/// <para>
/// Menu administration (list, detail) is guarded by <c>Ensa.Menu</c>. Rendering the menu of
/// the signed-in user deliberately is not - see <see cref="GetUserMenuAsync"/>.
/// </para>
/// </summary>
public class MenuAppService(
    IServiceProvider serviceProvider,
    IMenuRepository menuRepository,
    IPermissionManager permissionManager)
    : EnsaAppService(serviceProvider), IMenuAppService
{
    /// <inheritdoc />
    public async Task<MenuDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Menu.Default);

        var menu = await menuRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Menu), id);

        return ObjectMapper.Map<Menu, MenuDto>(menu);
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<MenuListDto>> GetListAsync(
        GetMenuListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Menu.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "SortOrder ASC");

        var total = await menuRepository.GetCountAsync(predicate, cancellationToken);

        var records = await menuRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Menu>, List<MenuListDto>>(records);

        return new PagedResultDto<MenuListDto>(total, items);
    }

    /// <summary>
    /// Returns the menu of the signed-in user for the requested layout type.
    /// <para>
    /// <b>No permission is checked here, only an authenticated session.</b> This endpoint feeds
    /// the navigation shell that every user sees immediately after signing in. Guarding it with
    /// <c>Ensa.Menu</c> would be wrong twice over: that constant is the menu <i>administration</i>
    /// permission, so ordinary users - who are the ones that actually need this call - would get
    /// an empty navigation bar and no way to reach any screen.
    /// </para>
    /// <para>
    /// Confidentiality does not depend on the permission check here. The repository builds the
    /// tree <i>for this user id</i>: it matches the menu against the staff role, organization
    /// type and subscription plan, drops items whose module is not enabled for the company, drops
    /// items whose permission the user does not hold, and applies the personal
    /// <c>UserMenuOverride</c> rows. A caller therefore cannot see an entry it is not entitled to,
    /// and the user id comes from the validated token rather than from the request, so one user
    /// cannot ask for the menu of another.
    /// </para>
    /// <para>
    /// The effective permission set is resolved here rather than in the repository so that the
    /// legacy four-gate algorithm has exactly one implementation, in <c>IPermissionManager</c>.
    /// It governs what the navigation SHOWS; what a request may DO is decided independently by
    /// the endpoint gate. See ADR-041.
    /// </para>
    /// </summary>
    public async Task<MenuNavigationDto> GetUserMenuAsync(
        string menuTypeCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(menuTypeCode);

        // Throws EnsaAuthorizationException ("Ensa:NotAuthenticated") when there is no session.
        var userId = GetRequiredUserId();

        var code = menuTypeCode.Trim();

        var effectivePermissionIds =
            await permissionManager.GetEffectivePermissionIdsAsync(userId, cancellationToken);

        var navigation = await menuRepository
                             .GetUserMenuOverrideAsync(userId, code, effectivePermissionIds, cancellationToken)
                         ?? throw new BusinessException(
                                 "No menu is defined for this layout type.",
                                 "Ensa:Menu:NotFoundForUser")
                             .WithData("MenuTypeCode", code);

        return new MenuNavigationDto
        {
            Menu = ObjectMapper.Map<Menu, MenuDto>(navigation.Menu),
            MenuType = navigation.MenuType is { } menuType
                ? new LookupDto
                {
                    Id = menuType.Id,
                    DisplayName = menuType.Name,
                    Code = menuType.Code,
                    IsActive = menuType.IsActive
                }
                : null,
            Roots = [.. navigation.Roots.Select(MapNode)],
            ElementRoots = [.. navigation.ElementRoots.Select(MapElement)]
        };
    }

    // ----------------------------------------------------------- internals

    /// <summary>
    /// Projects one placement node and its subtree. The URL and icon fallbacks
    /// (placement first, catalogue entry second) are resolved here so the client never has to
    /// know that two sources exist.
    /// </summary>
    private static MenuNodeNavigationDto MapNode(MenuNodeNavigation source)
        => new()
        {
            Id = source.MenuNode.Id,
            MenuItemId = source.MenuNode.MenuItemId,
            ParentMenuNodeId = source.MenuNode.ParentMenuNodeId,
            MenuItemCode = source.MenuItem.Code,
            Title = source.Title,
            Url = source.EffectiveUrl,
            IconCssClass = source.EffectiveIconCssClass,
            CssClass = source.MenuNode.CssClass ?? source.MenuItem.CssClass,
            CssClass2 = source.MenuNode.CssClass2 ?? source.MenuItem.CssClass2,
            ModuleId = source.MenuItem.ModuleId,
            SortOrder = source.MenuNode.SortOrder,
            UserHidden = source.HiddenForUser,
            Children = [.. source.ChildNodes.Select(MapNode)]
        };

    /// <summary>Projects one free-form element and its subtree (legacy <c>MenuElement_T</c>).</summary>
    private static MenuElementNavigationDto MapElement(MenuElementNavigation source)
        => new()
        {
            Id = source.MenuElement.Id,
            ParentMenuElementId = source.MenuElement.ParentMenuElementId,
            Text = source.MenuElement.Text,
            IconCssClass = source.MenuElement.IconCssClass,
            CssClass = source.MenuElement.CssClass,
            CssStyle = source.MenuElement.CssStyle,
            Url = source.MenuElement.Url,
            UrlParameters = source.MenuElement.UrlParameters,
            SortOrder = source.MenuElement.SortOrder,
            Children = [.. source.ChildNodes.Select(MapElement)]
        };

    private static Expression<Func<Menu, bool>> BuildFilter(GetMenuListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var menuTypeCode = string.IsNullOrWhiteSpace(input.MenuTypeCode) ? null : input.MenuTypeCode.Trim();
        var userTypeCode = string.IsNullOrWhiteSpace(input.UserTypeCode) ? null : input.UserTypeCode.Trim();
        var organizationTypeId = input.OrganizationTypeId;
        var subscriptionPlanId = input.SubscriptionPlanId;
        var isActive = input.IsActive;

        return m =>
            (search == null || m.Name.Contains(search))
            && (menuTypeCode == null || m.MenuTypeCode == menuTypeCode)
            && (userTypeCode == null || m.UserTypeCode == userTypeCode)
            && (organizationTypeId == null || m.OrganizationTypeId == organizationTypeId)
            && (subscriptionPlanId == null || m.SubscriptionPlanId == subscriptionPlanId)
            && (isActive == null || m.IsActive == isActive);
    }
}
