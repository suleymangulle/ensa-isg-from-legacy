using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Item of a hierarchical code list (e.g. a legislation clause, a hazard sub-category).
/// <para>Legacy equivalent: <c>TreeItem_T</c>.</para>
/// <para>
/// The legacy parent relationship was expressed through the free-text
/// <c>ParentTreeItemCode</c> column; that column is retained for compatibility and a
/// normalised <see cref="ParentTreeNodeId"/> FK has been added alongside it. The same applies
/// to the <see cref="TreeCode"/>/<see cref="TreeId"/> pair.
/// </para>
/// <para>Host-level (tenant-less) reference table.</para>
/// </summary>
public class TreeNode : FullAuditedEntity
{
    /// <summary>Code of the owning tree (legacy compatibility). (Legacy: <c>TreeCode</c>)</summary>
    public string TreeCode { get; set; } = string.Empty;

    /// <summary>Owning tree. Normalised FK — no navigation property.</summary>
    public int? TreeId { get; set; }

    /// <summary>This item's own code. (Legacy: <c>TreeItemCode</c>)</summary>
    public string TreeNodeCode { get; set; } = string.Empty;

    /// <summary>Code of the parent item (legacy compatibility). (Legacy: <c>ParentTreeItemCode</c>)</summary>
    public string? ParentTreeNodeCode { get; set; }

    /// <summary>Parent item. Normalised self-referencing FK — no navigation property.</summary>
    public int? ParentTreeNodeId { get; set; }

    /// <summary>Item name. (Legacy: <c>TreeItemName</c>)</summary>
    public string TreeNodeName { get; set; } = string.Empty;

    /// <summary>Whether this is the root (top-level) item of the tree. (Legacy: <c>MainTreeItem</c>)</summary>
    public bool MainItem { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
