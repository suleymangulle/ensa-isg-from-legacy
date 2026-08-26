using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Membership.Dtos.Navigations;

/// <summary>
/// Everything the user detail screen needs in a single call: the user plus its
/// organization, offices, roles, effective permissions and location lookups.
/// <para>
/// Mirrors <c>Ensa.Domain.Membership.Navigations.UserNavigation</c>. Plain DTOs may not
/// declare class-typed properties, so the combination lives in a
/// <see cref="NavigationDto"/> derivative (see docs/ARCHITECTURE.md §4).
/// </para>
/// </summary>
public class UserNavigationDto : NavigationDto
{
    public UserDto User { get; set; } = null!;

    /// <summary>Organization (tenant) the user belongs to. <c>null</c> for host users.</summary>
    public LookupDto? Organization { get; set; }

    /// <summary>Default office taken from <c>User.OfficeId</c>.</summary>
    public LookupDto? Office { get; set; }

    /// <summary>Every office the user is assigned to through <c>UserOffice</c>.</summary>
    public List<LookupDto> Offices { get; set; } = [];

    /// <summary>Office assignments together with their monthly committed duration.</summary>
    public List<UserOfficeAssignmentDto> OfficeAssignments { get; set; } = [];

    /// <summary>Identity roles held by the user.</summary>
    public List<LookupDto> Roles { get; set; } = [];

    /// <summary>
    /// Effective permissions computed by <c>IPermissionManager</c> — staff-role defaults and
    /// explicit grants, filtered by the subscription-plan and organization-type gates, with
    /// explicit denials removed.
    /// </summary>
    public List<PermissionDto> Permissions { get; set; } = [];

    /// <summary>Staff-role metadata (display name / icon) matching <c>User.StaffRole</c>.</summary>
    public LookupDto? UserType { get; set; }

    /// <summary>Staff role of the user, repeated here so the header can render without the root DTO.</summary>
    public StaffRole StaffRole { get; set; }

    public LookupDto? City { get; set; }
    public LookupDto? District { get; set; }

    /// <summary>Organization ids the user can reach (multi-tenant service accounts).</summary>
    public List<int> OrganizationIds { get; set; } = [];

    /// <summary>Size of the profile photo in bytes.</summary>
    public long? PhotoSizeBytes { get; set; }
}

/// <summary>One user-to-office assignment with its monthly committed working time.</summary>
public class UserOfficeAssignmentDto : EntityDto
{
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public int MonthlyWorkDurationMinutes { get; set; }
}
