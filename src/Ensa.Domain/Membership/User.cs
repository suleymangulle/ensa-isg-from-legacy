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
public class User : IdentityUser<int>, IEntity<int>, IMultiTenant
{
    // ---- Identity / personal details ----

    // ---- Duty / employment ----

    // ---- Relationships ----

    // ---- Status / session ----

    // ---- Medula (SSI) integration ----

    // ---- IMultiTenant ----

    /// <summary>(Legacy: KurumId)</summary>
    public int? TenantId { get; set; }

    // ---- IFullAudited (implemented by hand because the base class cannot be inherited) ----

    // ---- IEntity<int> ----

    public object?[] GetKeys() => [Id];

    public override string ToString() => $"[{nameof(User)}] Id = {Id}";
}
