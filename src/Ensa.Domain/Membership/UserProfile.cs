using Ensa.Domain.Common;

namespace Ensa.Domain.Membership;

/// <summary>
/// Everything about a person that ASP.NET Core Identity does not own.
/// <para>
/// <b>Why this exists.</b> The <c>User</c> table had grown to 49 columns: fifteen belonging to
/// <c>IdentityUser</c> and thirty-four belonging to Ensa. The identity contract allows exactly one
/// application-specific property on the Identity user — <c>TenantId</c> — so the rest moves out.
/// This is where the person lives; <c>User</c> keeps only the account.
/// </para>
/// <para>
/// <b>Not the same thing as an account.</b> Name, address and photograph describe a human being.
/// Whether they can sign in, when they were locked out and what their password hash is describe a
/// credential. They change for different reasons, are read by different code, and belong in
/// different tables.
/// </para>
/// <para>
/// One row per user. <c>UserId</c> is unique, so this is a 1-1 extension rather than a history.
/// </para>
/// </summary>
public class UserProfile : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The account this profile belongs to. FK — no navigation property.</summary>
    public int UserId { get; set; }

    /// <summary>
    /// The client company this user belongs to, when they are a customer rather than staff.
    /// FK — no navigation property.
    /// <para>
    /// It is the key the company scope filter reads. It sits here rather than on the account
    /// because the Identity user carries nothing of ours but <c>TenantId</c>; who somebody works
    /// for is a fact about the person, not about their credential.
    /// </para>
    /// (Legacy: <c>Kullanici_T.FirmaId</c>)
    /// </summary>
    public int? CompanyId { get; set; }

    /// <summary>(Legacy: <c>Kullanici_T.Adi</c>)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>(Legacy: <c>Kullanici_T.Soyadi</c>)</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Turkish identity number. Encrypted at rest, deterministically, so it can still be searched
    /// for and constrained to be unique. (Legacy: <c>Kullanici_T.TCKimlikNo</c>)
    /// </summary>
    public string? NationalId { get; set; }

    /// <summary>(Legacy: <c>Kullanici_T.Adres</c>)</summary>
    public string? Address { get; set; }

    /// <summary>Province. FK — no navigation property. (Legacy: <c>SehirId</c>)</summary>
    public int? CityId { get; set; }

    /// <summary>District. FK — no navigation property. (Legacy: <c>IlceId</c>)</summary>
    public int? DistrictId { get; set; }

    /// <summary>
    /// Profile photograph, in the central document store. FK — no navigation property.
    /// <para>
    /// The legacy table held the image itself in an <c>image</c> column alongside the user's name
    /// and address, which meant every query that wanted a user name could drag a photograph across
    /// the wire. (Legacy: <c>Kullanici_T.Resim</c>)
    /// </para>
    /// </summary>
    public int? PhotoDocumentId { get; set; }

    /// <summary>Colour used to distinguish this user on the calendar. (Legacy: <c>Renk</c>)</summary>
    public string? Color { get; set; }

    /// <summary>
    /// Whether the account may be used. Distinct from Identity's lockout, which is temporary and
    /// automatic; this is a decision somebody made. (Legacy: <c>Aktif</c>)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The user must set a new password before doing anything else. Set for every migrated user:
    /// their legacy password was recoverable by anyone with database access, so it is a way in,
    /// not a secret. (Legacy: <c>SifreDegisti</c>)
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>(Legacy: <c>SozlesmeOnaylandi</c>)</summary>
    public bool IsContractApproved { get; set; }

    public override string ToString() => $"[{nameof(UserProfile)}] UserId = {UserId}";
}
