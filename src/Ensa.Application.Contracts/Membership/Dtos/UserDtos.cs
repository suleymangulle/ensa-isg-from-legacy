using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Membership.Dtos;

/// <summary>User list row — only the columns rendered in the grid.</summary>
public class UserListDto : EntityDto
{
    public string UserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Computed display name (<c>Name + LastName</c>).</summary>
    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gsm { get; set; }
    public StaffRole StaffRole { get; set; }
    public int? OfficeId { get; set; }
    public int? CompanyId { get; set; }
    public bool IsActive { get; set; }
    public bool OfficeAdmin { get; set; }
    public bool OrganizationAdmin { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
}

/// <summary>
/// User detail view.
/// <para>
/// Security note: <c>PasswordHash</c>, <c>SecurityStamp</c>, <c>MedulaPassword</c> and the
/// national id are never exposed here. Credentials leave the server only as write-only input.
/// </para>
/// </summary>
public class UserDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string UserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gsm { get; set; }

    public string? Address { get; set; }
    public int? CityId { get; set; }
    public int? DistrictId { get; set; }

    public int? PhotoDocumentId { get; set; }
    public string? Color { get; set; }

    public StaffRole StaffRole { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public decimal? GrossSalary { get; set; }
    public bool PartTime { get; set; }
    public int? MonthlyWorkDurationMinutes { get; set; }

    public int? OfficeId { get; set; }
    public bool OfficeAdmin { get; set; }
    public int? CompanyId { get; set; }
    public int? PermissionGroupId { get; set; }

    public bool IsActive { get; set; }
    public bool OrganizationAdmin { get; set; }
    public bool SystemAdministrator { get; set; }
    public bool IsContractApproved { get; set; }
    public bool MustChangePassword { get; set; }

    /// <summary>Workplace physician speciality code used by the Medula (SSI) integration.</summary>
    public string? MedicalSpecialtyCode { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }
}

/// <summary>
/// Fields shared by user create and update. Declared once so that a new field can never
/// drift between the two payloads.
/// <para>
/// <see cref="UserDto.SystemAdministrator"/> and <see cref="UserDto.OrganizationAdmin"/> are
/// deliberately absent: privilege elevation must not be possible through an ordinary user
/// form. Those flags are granted through role assignment instead.
/// </para>
/// </summary>
public abstract class UserInputDto
{
    [Required]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Email)]
    public string? Email { get; set; }

    [Phone]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? PhoneNumber { get; set; }

    [Phone]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Phone)]
    public string? Gsm { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.NationalId)]
    public string? NationalId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Address)]
    public string? Address { get; set; }

    public int? CityId { get; set; }
    public int? DistrictId { get; set; }

    public int? PhotoDocumentId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Color)]
    public string? Color { get; set; }

    public StaffRole StaffRole { get; set; } = StaffRole.Unspecified;

    public DateTime? HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }

    [Range(0, 9999999)]
    public decimal? GrossSalary { get; set; }

    public bool PartTime { get; set; }

    [Range(0, 100000)]
    public int? MonthlyWorkDurationMinutes { get; set; }

    public int? OfficeId { get; set; }
    public bool OfficeAdmin { get; set; }
    public int? CompanyId { get; set; }
    public int? PermissionGroupId { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Code)]
    public string? MedicalSpecialtyCode { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>User create input — the only payload that carries a password.</summary>
public class CreateUserDto : UserInputDto
{
    [Required]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.ShortName)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Initial password. Validated by the ASP.NET Core Identity password policy, never
    /// stored in clear text and never returned by any read endpoint.
    /// </summary>
    [Required]
    [MinLength(6)]
    [MaxLength(128)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Organization the user belongs to.
    /// <para>
    /// Only a <b>host</b> caller may set it. For a caller inside an organization the value is
    /// ignored and the user is created in that caller's own organization — a tenant must never
    /// be able to place a user in another one. Leaving it empty as a host caller creates another
    /// host user, which only makes sense for an administrator who manages every organization.
    /// </para>
    /// </summary>
    public int? TenantId { get; set; }

    /// <summary>Role names assigned right after creation. Unknown names are rejected.</summary>
    public string[] Roles { get; set; } = [];
}

/// <summary>
/// User update input.
/// <para>
/// Contains <b>no password</b> on purpose: a password change goes through
/// <c>IAccountAppService.ChangePasswordAsync</c> (self service) or
/// <c>IUserAppService.ResetPasswordAsync</c> (administrative reset), so an ordinary
/// profile update can never silently rewrite a credential. The user name is immutable
/// as well because it is the login identifier and appears in audit trails.
/// </para>
/// </summary>
public class UpdateUserDto : UserInputDto;

/// <summary>User list filter.</summary>
public class GetUserListInput : PagedAndSortedFilterDto
{
    public StaffRole? StaffRole { get; set; }
    public int? OfficeId { get; set; }
    public int? CompanyId { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>Administrative password reset payload.</summary>
public class ResetPasswordDto
{
    [Required]
    [MinLength(6)]
    [MaxLength(128)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>Role assignment payload — replaces the whole role set of the user.</summary>
public class AssignRolesDto
{
    [Required]
    public string[] Roles { get; set; } = [];
}

/// <summary>Activation / deactivation payload.</summary>
public class SetActiveStateDto
{
    public bool IsActive { get; set; }
}
