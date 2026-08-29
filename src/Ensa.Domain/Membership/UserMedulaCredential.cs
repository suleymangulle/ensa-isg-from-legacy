using Ensa.Domain.Common;

namespace Ensa.Domain.Membership;

/// <summary>
/// A physician's credentials for MEDULA, the national prescription service.
/// <para>
/// <b>Why these are not on the user.</b> They are a login for somebody else's system. Keeping them
/// beside the account's own password hash invites the two to be confused, and it puts a secret in
/// the table that almost every query in the application touches. 297 users have one; the other
/// 3,589 were carrying three empty columns.
/// </para>
/// <para>
/// <b>Encrypted, not hashed.</b> The password has to be sent to MEDULA, so it must be recoverable
/// — which is exactly why it does not belong in the same place as a password that must not be.
/// </para>
/// </summary>
public class UserMedulaCredential : FullAuditedTenantEntity
{
    /// <summary>The account these credentials belong to. FK — no navigation property.</summary>
    public int UserId { get; set; }

    /// <summary>(Legacy: <c>MedulaKullanici</c>)</summary>
    public string? MedulaUserName { get; set; }

    /// <summary>Encrypted at rest. (Legacy: <c>MedulaSifre</c>)</summary>
    public string? MedulaPassword { get; set; }

    /// <summary>The physician's branch code at MEDULA. (Legacy: <c>BransKodu</c>)</summary>
    public string? MedicalSpecialtyCode { get; set; }

    public override string ToString() => $"[{nameof(UserMedulaCredential)}] UserId = {UserId}";
}
