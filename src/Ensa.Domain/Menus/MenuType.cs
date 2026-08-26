using Ensa.Domain.Common;

namespace Ensa.Domain.Menus;

/// <summary>
/// A menu layout type: side menu, top menu, quick access and so on.
/// Legacy: <c>MenuType_T</c>.
/// <para>A host reference table; it does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class MenuType : AuditedEntity, IActivatable, IHasSortOrder
{
    /// <summary>Unique code; <see cref="Menu.MenuTypeCode"/> refers to this value. (Legacy: MenuTypeCode)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>(Legacy: MenuTypeAdi)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Code of the application or project the menu belongs to, for multi-application installations. (Legacy: ProjectCode)</summary>
    public string? ProjectCode { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
