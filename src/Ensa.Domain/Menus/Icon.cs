using Ensa.Domain.Common;

namespace Ensa.Domain.Menus;

/// <summary>
/// A single icon record — the catalogue row behind the icon picker screens.
/// Legacy: <c>Icon_T</c>.
/// <para>
/// A pure lookup table — thousands of rows, no audit fields needed — so it derives from
/// <see cref="Entity"/>. It is a host table and does NOT implement <see cref="IMultiTenant"/>.
/// </para>
/// </summary>
public class Icon : Entity, IHasSortOrder
{
    /// <summary>Code of the owning library — <see cref="IconLibrary.Code"/>. (Legacy: IconLibraryCode)</summary>
    public string LibraryCode { get; set; } = string.Empty;

    /// <summary>The CSS class that renders the icon, e.g. <c>fa fa-user</c>. (Legacy: IconClass)</summary>
    public string IconCssClass { get; set; } = string.Empty;

    /// <summary>
    /// Library-specific extra feature flag. Legacy held it as <c>ExtraProp bool?</c> and no business
    /// code ever read it; it was carried over so that no data is lost.
    /// (Legacy: ExtraProp)
    /// </summary>
    public bool ExtraFeature { get; set; }

    public int SortOrder { get; set; }
}
