using Ensa.Domain.Common;

namespace Ensa.Domain.Membership;

/// <summary>
/// Binds one API endpoint to the permission that guards it.
/// <para>
/// <b>Why this is data and not an attribute.</b> The legacy application never wrote a permission
/// name in its code. <c>PermissionCheck.Authorize</c> asked the runtime which method it was in and
/// looked that up in <c>Yetki_T.YetkiHedefi</c> — the permission table itself said which page or
/// method it guarded. The controller had no idea what it required, which is what made the
/// permission catalogue editable without a rebuild. This table restores that: the endpoint is
/// identified at request time from the routing metadata, and the answer to "which permission does
/// it need" comes from here.
/// </para>
/// <para>
/// <b>Why an endpoint may map to nothing.</b> <see cref="PermissionId"/> is nullable, and null does
/// not mean "unknown" — it means "authenticated is enough, on purpose". Signing in, reading your own
/// profile, changing your own password and fetching the navigation menu are all things a user with
/// no permissions at all must still be able to do; requiring a permission there would be wrong
/// rather than merely strict. An endpoint that is <i>absent</i> from this table is the different
/// case, and it is refused — the same way the legacy code refused a method with no matching
/// <c>Yetki_T</c> row ("Bu eylem henüz kullanıma açılmamış").
/// </para>
/// <para>
/// Host level, like <see cref="Permission"/> itself: the catalogue is shared by every tenant, so
/// there is no <c>TenantId</c> here. A tenant narrows what its users may do through the gates on
/// the permission, never by rewiring which endpoint needs which permission.
/// </para>
/// </summary>
public class PermissionEndpoint : AuditedEntity
{
    /// <summary>
    /// Controller name as ASP.NET Core reports it in the route metadata — without the
    /// <c>Controller</c> suffix, exactly as <c>ControllerActionDescriptor.ControllerName</c> gives
    /// it, so no string surgery is needed at request time.
    /// </summary>
    public string ControllerName { get; set; } = string.Empty;

    /// <summary>
    /// Action method name, as <c>ControllerActionDescriptor.ActionName</c> reports it.
    /// </summary>
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    /// The permission that guards this endpoint. FK — no navigation property.
    /// <para>
    /// <c>null</c> is a decision, not a gap: this endpoint deliberately needs nothing beyond a
    /// valid token. See the type documentation.
    /// </para>
    /// </summary>
    public int? PermissionId { get; set; }

    public override string ToString()
        => $"[{nameof(PermissionEndpoint)}] {ControllerName}.{ActionName} -> {PermissionId?.ToString() ?? "(authenticated only)"}";
}
