using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Tenancy;

namespace Ensa.Domain.Membership.Navigations;

/// <summary>
/// Combined read model for a user together with the organization, office(s), role(s),
/// effective permission(s) and location lookups.
/// <para>
/// <c>[NotMapped]</c> — never exposed as a <c>DbSet</c> and never registered with
/// <c>ModelBuilder</c>; it is populated in the repository layer through an
/// <c>IQueryable</c> join plus projection.
/// </para>
/// <para>
/// The <c>[NotMapped]</c> members of the legacy <c>User_T</c> are carried here:
/// <c>OrganizationIds</c> → <see cref="OrganizationIds"/>, <c>DocumentBoyutu</c> →
/// <see cref="PhotoDocumentBoyutu"/>. (The <c>Apply</c> member was a screen-local temporary flag
/// and was not even carried into the DTO.)
/// </para>
/// </summary>
[NotMapped]
public class UserNavigation : NavigationEntity<User>
{
    /// <summary>Shortcut to the root record.</summary>
    public User User
    {
        get => Entity;
        set => Entity = value;
    }

    /// <summary>The tenant the user belongs to.</summary>
    public Organization? Organization { get; set; }

    /// <summary>Default office (<c>User.OfficeId</c>).</summary>
    public Office? Office { get; set; }

    /// <summary>Every office the user is assigned to through <see cref="UserOffice"/>.</summary>
    public List<Office> Offices { get; set; } = [];

    /// <summary>Office assignments, including the monthly duration of each.</summary>
    public List<UserOffice> OfficeAssignments { get; set; } = [];

    /// <summary>Roles assigned to the user.</summary>
    public List<Role> Roles { get; set; } = [];

    /// <summary>The user's EFFECTIVE permissions — computed by <c>IPermissionManager</c>.</summary>
    public List<Permission> Permissions { get; set; } = [];

    /// <summary>User type metadata (name/icon) — the counterpart of <c>User.StaffRole</c>.</summary>
    /// <summary>
    /// The person. Carried here because the screen that reads this navigation shows a name, an
    /// address and a photograph, and none of those are on the account any more.
    /// </summary>
    public UserProfile? Profile { get; set; }

    /// <summary>The contract, for the hire date, the salary and the user type link.</summary>
    public UserEmployment? Employment { get; set; }

    public UserType? UserType { get; set; }

    /// <summary>Province name (lookup — the <c>City</c> table is defined in another module).</summary>
    public string? CityName { get; set; }

    /// <summary>District name (lookup).</summary>
    public string? DistrictName { get; set; }

    /// <summary>
    /// Ids of the organizations the user can reach (for system/representative accounts with
    /// multi-tenant access). (Legacy: <c>Kullanici_T.KurumIdleri</c> <c>[NotMapped]</c>)
    /// </summary>
    public List<int> OrganizationIds { get; set; } = [];

    /// <summary>Size of the profile photo in bytes. (Legacy: <c>DosyaBoyutu</c> <c>[NotMapped]</c>)</summary>
    public long? PhotoDocumentBoyutu { get; set; }
}
