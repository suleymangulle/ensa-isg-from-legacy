using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Root of a hierarchical code list (e.g. a legislation clause tree, a hazard classification
/// tree).
/// <para>Legacy equivalent: <c>Tree_T</c>.</para>
/// <para>Host-level (tenant-less) reference table.</para>
/// </summary>
public class Tree : AuditedEntity
{
    /// <summary>Unique tree code. (Legacy: <c>TreeCode</c>)</summary>
    public string TreeCode { get; set; } = string.Empty;

    /// <summary>Tree name. (Legacy: <c>TreeName</c>)</summary>
    public string TreeName { get; set; } = string.Empty;
}
