using Ensa.Domain.Common;

namespace Ensa.Domain.Menus;

/// <summary>
/// Page-to-menu mapping. It decides which menu is shown, and in which layout region, when a URL
/// is opened.
/// Legacy: <c>MenuPage_T</c>.
/// <para>A host reference table; it does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class MenuPage : CreationAuditedEntity, IActivatable
{
    /// <summary>Uygulama/proje kodu. (Legacy: ProjectCode)</summary>
    public string? ProjectCode { get; set; }

    /// <summary>Code of the menu to display. (Legacy: MenuCode)</summary>
    public string MenuCode { get; set; } = string.Empty;

    /// <summary>The mapped page address. (Legacy: PageUrl)</summary>
    public string PageUrl { get; set; } = string.Empty;

    /// <summary>Code of the layout region the menu occupies on the page: left, top, right and so on. (Legacy: MenuLocationCode)</summary>
    public string? SettlementCode { get; set; }

    /// <summary>(Legacy: Aktif)</summary>
    public bool IsActive { get; set; } = true;
}
