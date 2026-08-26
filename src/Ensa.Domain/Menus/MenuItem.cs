using Ensa.Domain.Common;

namespace Ensa.Domain.Menus;

/// <summary>
/// The catalogue of reusable menu entries: the title, icon and URL for a page or action. The same
/// entry can be placed into several menus through <see cref="MenuNode"/>.
/// Legacy: <c>MenuItem_T</c>.
/// <para>
/// A host reference table — ARCHITECTURE §5 lists it among the tenant-less tables — so it does NOT
/// implement <see cref="IMultiTenant"/>.
/// </para>
/// </summary>
public class MenuItem : AuditedEntity, IActivatable, IHasSortOrder
{
    /// <summary>Unique entry code. (Legacy: MenuItemCode)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Title shown in the menu. (Legacy: MenuItemAdi)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Code of the application or project the entry belongs to. (Legacy: ProjectCode)</summary>
    public string? ProjectCode { get; set; }

    /// <summary>Short description or subtitle. (Legacy: Aciklama1)</summary>
    public string? Description1 { get; set; }

    /// <summary>Secondary short description or badge text. (Legacy: Aciklama2)</summary>
    public string? Description2 { get; set; }

    /// <summary>Long description, used for the help tooltip. (Legacy: UzunAciklama)</summary>
    public string? LongDescription { get; set; }

    /// <summary>Target address of the link. (Legacy: URL)</summary>
    public string? Url { get; set; }

    /// <summary>Query string keys to append to the URL, comma separated. (Legacy: QueryStringKeywords)</summary>
    public string? QueryStringKeys { get; set; }

    /// <summary>Extra HTML attributes rendered onto the anchor tag. (Legacy: Attrs)</summary>
    public string? ExtraAttributes { get; set; }

    /// <summary>Icon CSS class. (Legacy: Icon)</summary>
    public string? IconCssClass { get; set; }

    /// <summary>Primary CSS class. (Legacy: CssClass)</summary>
    public string? CssClass { get; set; }

    /// <summary>Secondary CSS class. (Legacy: CssClass2)</summary>
    public string? CssClass2 { get; set; }

    /// <summary>Inline style. (Legacy: CssStyle)</summary>
    public string? CssStyle { get; set; }

    /// <summary>
    /// FK to the module that makes the entry visible. When the module is not enabled for the company,
    /// the entry is hidden. Legacy used <c>-1</c> to mean "no module link"; the new model expresses
    /// that as <c>null</c>. (Legacy: ConnectedModule int?)
    /// </summary>
    public int? ModuleId { get; set; }

    /// <summary>Sort order. (Legacy: Index int? — renamed because "Index" is a problematic name in C# and EF.)</summary>
    public int SortOrder { get; set; }

    /// <summary>(Legacy: Aktif)</summary>
    public bool IsActive { get; set; } = true;
}
