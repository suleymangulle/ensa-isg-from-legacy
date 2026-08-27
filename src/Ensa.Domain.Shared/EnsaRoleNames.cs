namespace Ensa.Domain.Shared;

/// <summary>
/// Names of the roles the application itself reasons about.
/// <para>
/// They live in the shared layer because both ends need them and neither may reference the other:
/// the domain asks whether a user is a system administrator while deciding permissions, and the
/// host writes the same name into the token's role claims. One definition, so the two cannot drift.
/// </para>
/// <para>
/// These are <b>roles</b>, not permissions. They answer "what is this person", they are stored in
/// Identity's own <c>UserRole</c> table, and the legacy permission gates never consult them.
/// </para>
/// </summary>
public static class EnsaRoleNames
{
    /// <summary>
    /// Skips every gate — the legacy <c>SerAdmin</c>, whose check was the first line of
    /// <c>PermissionCheck.Authorize</c>: <c>if (Kullanici.SerAdmin) return;</c>
    /// </summary>
    public const string SystemAdministrator = "SystemAdministrator";

    /// <summary>Administers one tenant. (Legacy: <c>Kullanici_T.Admin</c>)</summary>
    public const string OrganizationAdministrator = "OrganizationAdministrator";

    /// <summary>Administers one office. (Legacy: <c>Kullanici_T.OfisAdmin</c>)</summary>
    public const string OfficeAdministrator = "OfficeAdministrator";
}
