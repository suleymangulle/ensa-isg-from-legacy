using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Menus.Dtos.Navigations;

/// <summary>
/// A menu together with its hierarchical item tree - ready to render.
/// <para>
/// Mirrors <c>Ensa.Domain.Menus.Navigations.MenuNavigation</c>. A recursive structure cannot
/// live on a plain DTO (no class-typed properties), so the tree is exposed through
/// <see cref="NavigationDto"/> derivatives (see docs/ARCHITECTURE.md section 4).
/// </para>
/// </summary>
public class MenuNavigationDto : NavigationDto
{
    public MenuDto Menu { get; set; } = null!;

    /// <summary>Layout type of the menu.</summary>
    public LookupDto? MenuType { get; set; }

    /// <summary>Root placements (<c>ParentMenuNodeId == null</c>), ordered.</summary>
    public List<MenuNodeNavigationDto> Roots { get; set; } = [];

    /// <summary>
    /// Free-form elements for menus built on the legacy <c>MenuElement_T</c> structure.
    /// Empty when the menu uses the shared <c>MenuItem</c> catalogue.
    /// </summary>
    public List<MenuElementNavigationDto> ElementRoots { get; set; } = [];
}

/// <summary>
/// One node of the menu tree: the placement (<c>MenuNode</c>) merged with the catalogue
/// entry (<c>MenuItem</c>) it renders, plus its children.
/// <para>
/// URL and icon are already resolved: the placement wins when it defines one, otherwise the
/// value of the catalogue entry is used. The client does not repeat that fallback logic.
/// </para>
/// </summary>
public class MenuNodeNavigationDto : NavigationDto
{
    /// <summary>Identifier of the placement row.</summary>
    public int Id { get; set; }

    public int MenuItemId { get; set; }

    public int? ParentMenuNodeId { get; set; }

    /// <summary>Catalogue code of the rendered item - used for client-side route guarding.</summary>
    public string MenuItemCode { get; set; } = string.Empty;

    /// <summary>Effective title, taken from the catalogue entry.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Effective URL: the value of the placement when set, otherwise of the catalogue entry.</summary>
    public string? Url { get; set; }

    /// <summary>Effective icon: the value of the placement when set, otherwise of the catalogue entry.</summary>
    public string? IconCssClass { get; set; }

    public string? CssClass { get; set; }
    public string? CssClass2 { get; set; }

    /// <summary>Module gate of the catalogue entry, if any.</summary>
    public int? ModuleId { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Child nodes, ordered. Empty for a leaf.</summary>
    public List<MenuNodeNavigationDto> Children { get; set; } = [];

    /// <summary>Whether the node opens a sub-menu - drives the drop-down styling.</summary>
    public bool ChildMenuExists => Children.Count > 0;

    /// <summary>
    /// Node hidden for this specific user through a <c>UserMenuOverride</c> row. Such nodes are
    /// absent from a rendered menu; the flag exists so the menu editor can show them as ticked.
    /// </summary>
    public bool UserHidden { get; set; }
}

/// <summary>A free-form menu element and its children (legacy <c>MenuElement_T</c> tree).</summary>
public class MenuElementNavigationDto : NavigationDto
{
    public int Id { get; set; }

    public int? ParentMenuElementId { get; set; }

    public string Text { get; set; } = string.Empty;

    public string? IconCssClass { get; set; }
    public string? CssClass { get; set; }
    public string? CssStyle { get; set; }

    public string? Url { get; set; }
    public string? UrlParameters { get; set; }

    public int SortOrder { get; set; }

    public List<MenuElementNavigationDto> Children { get; set; } = [];

    public bool ChildMenuExists => Children.Count > 0;
}
