using Ensa.Domain.Membership;

namespace Ensa.Application.Membership;

/// <summary>
/// Shortcuts used throughout the sign-in and authorization context, so that token issuing, the
/// profile screen and permission resolution all apply the same rules from a single place.
/// <para>
/// Two of these now take <see cref="UserAuthorizationFacts"/> rather than a <see cref="User"/>,
/// because the answers moved out of that row: whether an account may be used lives in
/// <see cref="UserProfile"/>, and being an administrator is a role assignment Identity owns.
/// Handing them a user and letting each one reach for its answer would put the decision of
/// "which table do I ask" at every call site. The repository asks once; these read the result.
/// </para>
/// </summary>
public static class UserExtensions
{
    /// <summary>
    /// Whether the user may sign in and be issued a token.
    /// No token is issued to a deleted or deactivated account.
    /// </summary>
    public static bool CanSignIn(this UserAuthorizationFacts facts) => facts.CanAct;

    /// <summary>
    /// Whether the user is a host (system) administrator. Legacy: <c>SerAdmin</c>, whose check was
    /// the first line of the legacy authorization routine.
    /// Such a user holds every permission and may switch tenants with <c>X-Ensa-TenantId</c>.
    /// </summary>
    public static bool IsHostAdmin(this UserAuthorizationFacts facts) => facts.IsSystemAdministrator;

    /// <summary>
    /// The user's organization (tenant) id. <c>null</c> means a host user.
    /// This is the source of the <c>ensa:tenantId</c> claim in the token.
    /// </summary>
    public static int? GetTenantId(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.TenantId;
    }

    /// <summary>
    /// Display name shown on screen; falls back to the user name when the profile has no name, or
    /// when there is no profile at all.
    /// </summary>
    public static string GetDisplayName(this User user, UserProfile? profile)
    {
        ArgumentNullException.ThrowIfNull(user);

        var fullName = $"{profile?.Name} {profile?.LastName}".Trim();

        return string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;
    }
}
