using Ensa.Domain.Common;

namespace Ensa.Domain.Ibys;

/// <summary>
/// IBYS work equipment top-level category.
/// <para>Legacy equivalent: <c>IBYSIsEquipmentParentKategorileri_T</c>.</para>
/// <para>Host reference table — does NOT implement <c>IMultiTenant</c>.</para>
/// </summary>
public class IbysEquipmentTopCategory : AuditedEntity, IActivatable
{
    /// <summary>Top-level category name. (Legacy: <c>UstKategoriAdi</c>)</summary>
    public string ParentCategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the category is still in use.
    /// (The legacy table had no such column; it was added for consistency and seeded as
    /// <c>true</c>.)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
