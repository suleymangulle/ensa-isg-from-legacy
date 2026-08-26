using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Companies;

/// <summary>
/// Service staff assigned to a company (OHS specialist / workplace physician / other health
/// personnel).
/// <para>Legacy equivalent: <c>AssignedSpecialist_T</c>.</para>
/// </summary>
public class AssignedSpecialist : FullAuditedTenantEntity, IActivatable, ICompanyScoped
{
    public int CompanyId { get; set; }

    /// <summary>The system user assigned (specialist/physician).</summary>
    public int UserId { get; set; }

    /// <summary>The user's role at this company. (Legacy: string)</summary>
    public StaffRole StaffRole { get; set; } = StaffRole.Unspecified;

    /// <summary>
    /// Monthly working time the legislation requires to be allocated to this company, in minutes.
    /// (Legacy: <c>AylikCalismaSuresi</c>)
    /// </summary>
    public int? MonthlyWorkDurationMinutes { get; set; }

    /// <summary>Legacy synchronisation/external system key. (Legacy: <c>SID</c>)</summary>
    public string? Sid { get; set; }

    /// <summary>Whether the assignment has been approved through İSG-Prof (İSG-KATİP).</summary>
    public bool OhsProfApproval { get; set; }

    /// <summary>Single-use key used in the İSG-Prof approval flow.</summary>
    public string? OhsProfApprovalGuid { get; set; }

    /// <summary>Whether the assignment is active. (Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
