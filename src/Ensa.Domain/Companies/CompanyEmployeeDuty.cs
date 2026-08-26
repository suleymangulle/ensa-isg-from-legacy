using Ensa.Domain.Common;

namespace Ensa.Domain.Companies;

/// <summary>
/// An OHS duty assigned to an employee (first aider, fire response team member, employee
/// representative, OHS board member, and so on).
/// <para>Legacy equivalent: <c>CompanyEmployeeDuty_T</c>.</para>
/// </summary>
public class CompanyEmployeeDuty : FullAuditedTenantEntity, IActivatable
{
    public int CompanyEmployeeId { get; set; }

    /// <summary>FK to the host <c>Duty</c> definition table.</summary>
    public int DutyId { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
