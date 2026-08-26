using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// A team member assigned in an emergency action plan.
/// (Legacy: <c>AcilDurumEylemPlaniPersoneli_T</c>)
/// <para>
/// Conversions: <c>StaffRole</c> string → the <see cref="Shared.Enums.StaffRole"/> enum;
/// <c>TeamType</c> string → the <see cref="EmergencyTeamType"/> enum.
/// </para>
/// </summary>
public class EmergencyTeamMember : FullAuditedTenantEntity
{
    /// <summary>FK → <see cref="EmergencyActionPlan"/>.</summary>
    public int EmergencyActionPlanId { get; set; }

    /// <summary>The assigned employee. FK → <c>CompanyEmployee.Id</c>.</summary>
    public int CompanyEmployeeId { get; set; }

    /// <summary>The employee's role. (Legacy: <c>PersonelTuru</c> string)</summary>
    public StaffRole StaffRole { get; set; } = StaffRole.Unspecified;

    /// <summary>The emergency team they belong to. (Legacy: <c>EkipTuru</c> string)</summary>
    public EmergencyTeamType TeamType { get; set; } = EmergencyTeamType.Unspecified;
}
