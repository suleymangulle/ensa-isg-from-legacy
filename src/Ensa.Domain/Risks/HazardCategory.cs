using Ensa.Domain.Common;

namespace Ensa.Domain.Risks;

/// <summary>
/// A category node in the hazard library. (Legacy: <c>TehlikeKategori_T</c>)
/// <para>
/// TENANCY DECISION: the legacy table had NO <c>OrganizationId</c> column — it was a reference
/// library shared by every organization, and <c>ARCHITECTURE.md §5</c> lists it among the host
/// tables. In practice, though, organizations are expected to add their own categories, so the
/// <see cref="AuditedTenantEntity"/> base (audit fields plus <c>TenantId</c>) was chosen:
/// <c>TenantId = null</c> → a SHARED (host) library row, <c>TenantId != null</c> → a row owned by
/// one organization. Legacy data is migrated with <c>TenantId = null</c> throughout. The global
/// query filter (<c>TenantId == CurrentTenant.Id || TenantId == null</c>) already suits this mix.
/// </para>
/// </summary>
public class HazardCategory : AuditedTenantEntity, IActivatable, IHasSortOrder
{
    /// <summary>Category name. (Legacy: <c>KategoriAdi</c>)</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Position of the category in the tree. (Legacy: <c>SiraNo</c>)
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// When <c>true</c> the category appears in the "hazard source" list; when <c>false</c> it appears
    /// in the hazard tree. (Legacy: <c>TehlikeKaynagi</c> bool)
    /// </summary>
    public bool IsHazardSource { get; set; }

    /// <summary>
    /// A free-form classification tag attached to the category in legacy. (Legacy: <c>DataType</c> string)
    /// Its values do not fall into a fixed set, so it was not converted to an enum.
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>Whether the category is active. (Not present on the legacy category table; added to manage the library.)</summary>
    public bool IsActive { get; set; } = true;
}
