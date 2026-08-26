using Ensa.Domain.Common;

namespace Ensa.Domain.Companies;

/// <summary>
/// An expense the organization/office incurred on company operations.
/// <para>Legacy equivalent: <c>CompanyExpense_T</c>.</para>
/// </summary>
public class OfficeExpense : FullAuditedTenantEntity
{
    /// <summary>Description/line item of the expense.</summary>
    public string ExpenseTag { get; set; } = string.Empty;

    /// <summary>Expense amount. (Legacy: <c>double</c>)</summary>
    public decimal Amount { get; set; }

    /// <summary>Date the expense was incurred.</summary>
    public DateTime? ExpenseDate { get; set; }

    /// <summary>The office that bears the expense.</summary>
    public int OfficeId { get; set; }
}
