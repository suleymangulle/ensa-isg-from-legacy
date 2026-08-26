using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;

namespace Ensa.Domain.Menus.Navigations;

/// <summary>
/// Combined view of a menu and its hierarchical entry tree — the render-ready menu for one user.
/// <para>
/// <c>[NotMapped]</c> — NEVER a <c>DbSet</c>, never added to <c>ModelBuilder</c>;
/// the repository layer runs a flat query and shapes the result into a tree in memory.
/// </para>
/// </summary>
[NotMapped]
public class MenuNavigation : NavigationEntity<Menu>
{
    /// <summary>Shortcut to the root record.</summary>
    public Menu Menu
    {
        get => Entity;
        set => Entity = value;
    }

    public MenuType? MenuType { get; set; }

    /// <summary>Root-level menu nodes (<c>ParentMenuNodeId == null</c>), in order.</summary>
    public List<MenuNodeNavigation> Roots { get; set; } = [];

    /// <summary>
    /// Free-form menu elements, for menus built on the legacy <c>MenuElement_T</c> shape.
    /// Left empty when the classic <see cref="MenuNode"/> shape is in use.
    /// </summary>
    public List<MenuElementNavigation> ElementRoots { get; set; } = [];
}

/// <summary>
/// A single node of the menu tree: the placement (<see cref="MenuNode"/>), the catalogue entry
/// (<see cref="MenuItem"/>) and the child nodes.
/// <para><c>[NotMapped]</c> — it has no database counterpart.</para>
/// </summary>
[NotMapped]
public class MenuNodeNavigation : NavigationEntity<MenuNode>
{
    /// <summary>Shortcut to the root record — this node's placement data.</summary>
    public MenuNode MenuNode
    {
        get => Entity;
        set => Entity = value;
    }

    /// <summary>The catalogue entry this node displays.</summary>
    public MenuItem MenuItem { get; set; } = null!;

    /// <summary>Child nodes, in order. An empty list means this is a leaf node.</summary>
    public List<MenuNodeNavigation> ChildNodes { get; set; } = [];

    /// <summary>The effective title — <see cref="MenuItem.Name"/>.</summary>
    public string Title => MenuItem.Name;

    /// <summary>The effective URL: the placement's own if it defines one, otherwise the catalogue entry's.</summary>
    public string? EffectiveUrl => string.IsNullOrWhiteSpace(MenuNode.Url) ? MenuItem.Url : MenuNode.Url;

    /// <summary>The effective icon: the placement's own if it defines one, otherwise the catalogue entry's.</summary>
    public string? EffectiveIconCssClass =>
        string.IsNullOrWhiteSpace(MenuNode.IconCssClass) ? MenuItem.IconCssClass : MenuNode.IconCssClass;

    /// <summary>Whether the node has children. (Legacy: <c>ThereIsSubList</c> — it drove the drop-down menu CSS.)</summary>
    public bool ChildMenuExists => ChildNodes.Count > 0;

    /// <summary>
    /// Whether this node is hidden for the specific user.
    /// (Legacy: <c>RemovedThisUser</c> — <c>UserMenu_T.OperationType == "removed"</c>)
    /// When a menu is built for rendering these nodes are not returned at all; the field is kept so
    /// that the menu EDITING screen can still show them as marked.
    /// </summary>
    public bool HiddenForUser { get; set; }
}

/// <summary>
/// A free-form menu element node and its children (the legacy <c>MenuElement_T</c> tree).
/// <para><c>[NotMapped]</c> — it has no database counterpart.</para>
/// </summary>
[NotMapped]
public class MenuElementNavigation : NavigationEntity<MenuElement>
{
    /// <summary>Shortcut to the root record.</summary>
    public MenuElement MenuElement
    {
        get => Entity;
        set => Entity = value;
    }

    /// <summary>Child elements, in order.</summary>
    public List<MenuElementNavigation> ChildNodes { get; set; } = [];

    public bool ChildMenuExists => ChildNodes.Count > 0;
}
