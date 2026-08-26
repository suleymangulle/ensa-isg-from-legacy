using Ensa.Domain.Common;

namespace Ensa.Domain.Menus;

/// <summary>
/// An icon library: Font Awesome, Metronic, Line Awesome and so on.
/// Legacy: <c>IconLibrary_T</c>.
/// <para>A host reference table; it does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class IconLibrary : Entity, IActivatable, IHasSortOrder
{
    /// <summary>Unique library code; <see cref="Icon.LibraryCode"/> refers to this value. (Legacy: LibraryCode)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>(Legacy: LibraryName)</summary>
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
