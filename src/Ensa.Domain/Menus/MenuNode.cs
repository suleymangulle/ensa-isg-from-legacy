using Ensa.Domain.Common;

namespace Ensa.Domain.Menus;

/// <summary>
/// The hierarchical placement of a <see cref="MenuItem"/> within one <see cref="Menu"/>.
/// Legacy: <c>MenuDetail_T</c>.
/// <para>
/// Because the same entry can appear in different menus at a different position, order and style,
/// the URL, icon and CSS fields can be OVERRIDDEN here; when they are <c>null</c>, the catalogue
/// entry's own values are used.
/// </para>
/// <para>A host reference table; it does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class MenuNode : AuditedEntity, IActivatable, IHasSortOrder
{
    public int MenuId { get; set; }

    public int MenuItemId { get; set; }

    /// <summary>
    /// FK to the parent node, which forms the hierarchy.
    /// In legacy this was <c>PrentMenuDetailId</c> — the typo was not carried over — typed <c>int</c>,
    /// with root nodes marked by <c>0</c>. In the new model a root node is <c>null</c>.
    /// </summary>
    public int? ParentMenuNodeId { get; set; }

    /// <summary>URL specific to this placement; when <c>null</c>, <see cref="MenuItem.Url"/> is used. (Legacy: URL)</summary>
    public string? Url { get; set; }

    /// <summary>Icon specific to this placement; when <c>null</c>, the catalogue entry's icon is used. (Legacy: Icon)</summary>
    public string? IconCssClass { get; set; }

    /// <summary>(Legacy: CssClass)</summary>
    public string? CssClass { get; set; }

    /// <summary>(Legacy: CssClass2)</summary>
    public string? CssClass2 { get; set; }

    /// <summary>Order among siblings at the same level. (Legacy: Index int?)</summary>
    public int SortOrder { get; set; }

    /// <summary>(Legacy: Aktif)</summary>
    public bool IsActive { get; set; } = true;
}
