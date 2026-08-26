using Ensa.Domain.Common;

namespace Ensa.Domain.Menus;

/// <summary>
/// A free-form menu element: a menu node that carries its own text, icon and URL and builds its
/// own hierarchy, without using the shared <see cref="MenuItem"/> catalogue.
/// Legacy: <c>MenuEleman_T</c>.
/// <para>
/// It is the alternative to <see cref="MenuNode"/>: where <c>MenuNode</c> places an entry from the
/// catalogue, <c>MenuElement</c> keeps all presentation data on its own row. Both attach to a
/// <see cref="Menu"/>.
/// </para>
/// <para>A host reference table; it does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class MenuElement : AuditedEntity, IActivatable, IHasSortOrder
{
    public int MenuId { get; set; }

    /// <summary>FK to the parent element, which forms the hierarchy. <c>null</c> means a root. (Legacy: MenuUstElId)</summary>
    public int? ParentMenuElementId { get; set; }

    /// <summary>Display text. (Legacy: ElMetin)</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Icon CSS class. (Legacy: ElIcon)</summary>
    public string? IconCssClass { get; set; }

    /// <summary>(Legacy: ElClass)</summary>
    public string? CssClass { get; set; }

    /// <summary>(Legacy: ElStyle)</summary>
    public string? CssStyle { get; set; }

    /// <summary>Hedef adres. (Legacy: UrlString)</summary>
    public string? Url { get; set; }

    /// <summary>URL'e eklenecek parametreler. (Legacy: UrlParams)</summary>
    public string? UrlParameters { get; set; }

    /// <summary>Order among siblings. (Legacy: IndexValue int?)</summary>
    public int SortOrder { get; set; }

    /// <summary>(Legacy: Aktif)</summary>
    public bool IsActive { get; set; } = true;
}
