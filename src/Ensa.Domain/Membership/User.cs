using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;
using Microsoft.AspNetCore.Identity;

namespace Ensa.Domain.Membership;

/// <summary>
/// System user. Legacy: <c>Kullanici_T</c> (PK <c>UserId</c>, tenant column <c>OrganizationId</c>).
/// <para>
/// Derives from <see cref="IdentityUser{TKey}"/>; members such as <c>UserName</c>, <c>Email</c>,
/// <c>PasswordHash</c>, <c>PhoneNumber</c>, <c>SecurityStamp</c>, <c>LockoutEnabled</c>,
/// <c>AccessFailedCount</c> and <c>TwoFactorEnabled</c> come from the base class and are NOT
/// redeclared here. <c>IdentityUser&lt;int&gt;</c> exposes no collection navigation properties,
/// so the "no navigation properties" rule is not violated.
/// </para>
/// <para>
/// Since it cannot inherit from <see cref="FullAuditedTenantEntity"/>, the tenant and audit
/// members are implemented BY HAND through the corresponding interfaces.
/// </para>
/// <para>
/// The legacy <c>Password</c> column (<c>[EncryptColumn]</c>) was NOT carried over → Identity
/// <c>PasswordHash</c>. Legacy <c>Phone</c> → Identity <c>PhoneNumber</c>. Legacy <c>Email</c> →
/// Identity <c>Email</c>. The legacy <c>[NotMapped]</c> members (<c>OrganizationIds</c>,
/// <c>Apply</c>, <c>DocumentBoyutu</c>) were removed from the entity; where needed they are
/// carried by <c>UserNavigation</c> or a DTO.
/// </para>
/// </summary>
public class User : IdentityUser<int>, IEntity<int>, IMultiTenant, IFullAudited, IActivatable, ICompanyScoped
{
    // ---- Identity / personal details ----

    public string Name { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>(Legacy: TCKimlikNo — <c>[EncryptColumn]</c>; stored encrypted.)</summary>
    public string? NationalId { get; set; }

    public string? Address { get; set; }

    public int? CityId { get; set; }

    public int? DistrictId { get; set; }

    /// <summary>Mobile phone. (Legacy: GSM — the landline <c>Phone</c> column moved to Identity <c>PhoneNumber</c>.)</summary>
    public string? Gsm { get; set; }

    /// <summary>User photo — FK to the central <c>Document</c> table. (Legacy: byte[] Resim + ResimDosyaAdi + ResimDosyaTuru)</summary>
    public int? PhotoDocumentId { get; set; }

    /// <summary>Colour (hex) representing the user on calendar/planning screens. (Legacy: Renk)</summary>
    public string? Color { get; set; }

    // ---- Duty / employment ----

    /// <summary>(Legacy: PersonelTuru string — matched <c>UserType_T.UserTypeCode</c>.)</summary>
    public StaffRole StaffRole { get; set; } = StaffRole.Unspecified;

    public DateTime? HireDate { get; set; }

    public DateTime? TerminationDate { get; set; }

    /// <summary>Gross salary. (Legacy: BrutMaas double → decimal)</summary>
    public decimal? GrossSalary { get; set; }

    /// <summary>Whether the user works part time. (Legacy: PartTime int? → bool)</summary>
    public bool PartTime { get; set; }

    /// <summary>Contracted monthly working time, in minutes. (Legacy: CalismaSuresi int?)</summary>
    public int? MonthlyWorkDurationMinutes { get; set; }

    // ---- Relationships ----

    /// <summary>FK of the user's default office. (Additional assignments live in <c>UserOffice</c>.)</summary>
    public int? OfficeId { get; set; }

    /// <summary>Whether the user administers their own office. (Legacy: OfisAdmin)</summary>
    public bool OfficeAdmin { get; set; }

    /// <summary>FK of the client company, when the user is a company user. (Legacy: FirmaId)</summary>
    public int? CompanyId { get; set; }

    /// <summary>FK of the permission group used for bulk permission assignment. (Legacy: YetkiGrubuId)</summary>
    public int? PermissionGroupId { get; set; }

    // ---- Status / session ----

    /// <summary>(Legacy: Aktif)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Organization administrator (tenant admin). (Legacy: Admin)</summary>
    public bool OrganizationAdmin { get; set; }

    /// <summary>System administrator — has access to every tenant and every permission. (Legacy: SerAdmin)</summary>
    public bool SystemAdministrator { get; set; }

    /// <summary>Whether the terms of use have been accepted. (Legacy: SozlesmeOnaylandi int? → bool)</summary>
    public bool ContractApproved { get; set; }

    /// <summary>Forces a password change on next sign-in. (Legacy: SifreDegisti int? → bool, MEANING INVERTED.)</summary>
    public bool MustChangePassword { get; set; }

    // ---- Medula (SSI) integration ----

    /// <summary>Specialty code of the workplace physician. (Legacy: BransKodu)</summary>
    public string? BranchCode { get; set; }

    /// <summary>(Legacy: MedulaKullanici — <c>[EncryptColumn]</c>; stored encrypted.)</summary>
    public string? MedulaUserName { get; set; }

    /// <summary>
    /// Medula password. <b>Stored encrypted</b> — column-level encryption via the EF Core
    /// <c>EncryptedStringConverter</c> lands in phase 2.
    /// (Legacy: MedulaSifre <c>[EncryptColumn]</c>)
    /// </summary>
    public string? MedulaPassword { get; set; }

    // ---- IMultiTenant ----

    /// <summary>(Legacy: KurumId)</summary>
    public int? TenantId { get; set; }

    // ---- IFullAudited (implemented by hand because the base class cannot be inherited) ----

    public DateTime CreationTime { get; set; }

    public int? CreatorId { get; set; }

    public DateTime? LastModificationTime { get; set; }

    public int? LastModifierId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletionTime { get; set; }

    public int? DeleterId { get; set; }

    // ---- IEntity<int> ----

    public object?[] GetKeys() => [Id];

    /// <summary>Full name used on screen. Computed — never persisted.</summary>
    public string FullName => $"{Name} {LastName}".Trim();

    public override string ToString() => $"[{nameof(User)}] Id = {Id}";
}
