using Ensa.Domain.Common;

namespace Ensa.Domain.Companies;

/// <summary>
/// Document supporting an employee duty (first aider certificate, assignment letter, and so on).
/// <para>Legacy equivalent: <c>CompanyEmployeeDutyDocument_T</c>.</para>
/// </summary>
public class CompanyEmployeeDutyDocument : FullAuditedTenantEntity, IActivatable
{
    public int CompanyEmployeeDutyId { get; set; }

    /// <summary>FK to the central <c>Document</c> table.</summary>
    public int DocumentId { get; set; }

    /// <summary>Date the document was issued.</summary>
    public DateTime DocumentDate { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
