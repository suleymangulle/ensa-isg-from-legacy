using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Tenancy;

/// <summary>
/// A sales rep — the host user who follows prospects and contracts.
/// Legacy: <c>Temsilci_T</c>.
/// <para>
/// In legacy this was a separate credential store (<c>KulName</c> + <c>Password</c>, both
/// <c>[EncryptColumn]</c>). In the new architecture a sales rep signs in through ASP.NET Core
/// Identity as well: the credentials hang off <c>User</c> via <see cref="UserId"/>, and this entity
/// carries NO password field.
/// </para>
/// <para>A host record; it does NOT implement <see cref="IMultiTenant"/>.</para>
/// </summary>
public class SalesRep : FullAuditedEntity, IActivatable
{
    public string Name { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// FK to the rep's system user. Sign-in, password and lockout data are managed through
    /// <c>User</c> (Identity).
    /// <c>null</c> means the rep has not been granted access to the system yet.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>(Legacy: TemTuru int)</summary>
    public SalesRepType SalesRepType { get; set; } = SalesRepType.Unspecified;

    public bool IsActive { get; set; } = true;
}
