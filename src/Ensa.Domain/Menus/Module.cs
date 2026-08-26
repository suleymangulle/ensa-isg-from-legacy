using Ensa.Domain.Common;

namespace Ensa.Domain.Menus;

/// <summary>
/// A hierarchical application module: Training, Risk Assessment, Health Surveillance and so on.
/// Legacy: <c>Modul_T</c>.
/// <para>
/// It is the outermost menu visibility filter: an entry with a <see cref="MenuItem.ModuleId"/> is
/// shown only when that module is enabled for the company through <see cref="CompanyModule"/>.
/// </para>
/// <para>A host reference table; it does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class Module : AuditedEntity, IActivatable, IHasSortOrder
{
    /// <summary>(Legacy: ModulAdi)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>FK to the parent module, which forms the hierarchy. <c>null</c> means a root module. (Legacy: UstModulId)</summary>
    public int? ParentModuleId { get; set; }

    /// <summary>(Legacy: Aktif)</summary>
    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
