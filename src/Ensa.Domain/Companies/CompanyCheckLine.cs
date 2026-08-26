using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Companies;

/// <summary>
/// Result of a single item on a company's monthly checklist.
/// <para>Legacy equivalent: <c>CompanyControlItemLine_T</c>.</para>
/// </summary>
public class CompanyCheckLine : AuditedTenantEntity
{
    /// <summary>The check header this line belongs to.</summary>
    public int CompanyControlItemId { get; set; }

    /// <summary>The check item definition.</summary>
    public int ControlItemId { get; set; }

    /// <summary>Whether the item was ticked (done/compliant). (Legacy: <c>KontrolDurum</c>)</summary>
    public bool ControlItemStatus { get; set; }

    /// <summary>Workflow status of the line. (Legacy: <c>Durum</c> string)</summary>
    public CompanyCheckStatus Status { get; set; } = CompanyCheckStatus.Active;
}
