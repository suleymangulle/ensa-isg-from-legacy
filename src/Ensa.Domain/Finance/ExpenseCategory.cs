using Ensa.Domain.Common;

namespace Ensa.Domain.Finance;

/// <summary>
/// Hierarchical expense category used for cash register outflows.
/// <para>Legacy equivalent: <c>ExitItem_T</c>.</para>
/// <para>
/// The tree is built through a self reference (<see cref="ParentExpenseCategoryId"/>);
/// navigation properties are NOT used.
/// </para>
/// </summary>
public class ExpenseCategory : AuditedTenantEntity, IActivatable
{
    public string Description { get; set; } = string.Empty;

    /// <summary>Parent category in the tree. FK — no navigation property.</summary>
    public int? ParentExpenseCategoryId { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
