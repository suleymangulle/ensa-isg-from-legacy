using Ensa.Domain.Membership;

namespace Ensa.Application.Membership;

/// <summary>
/// Shortcuts on <see cref="User"/> that are used throughout the sign-in and authorization context.
/// They keep the whole authentication flow (token issuing, profile, permission resolution) applying
/// the same rules from a single place.
/// </summary>
public static class UserExtensions
{
    /// <summary>
    /// Whether the user may sign in and be issued a token.
    /// No token is issued to a deleted (<c>IsDeleted</c>) or passive (<c>IsActive == false</c>) user.
    /// </summary>
    public static bool CanSignIn(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user is { IsDeleted: false, IsActive: true };
    }

    /// <summary>
    /// Whether the user is a host (system) administrator. Legacy: <c>SerAdmin</c>.
    /// Such a user holds every permission and may switch tenants with <c>X-Ensa-TenantId</c>.
    /// </summary>
    public static bool IsHostAdmin(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.SystemAdministrator;
    }

    /// <summary>
    /// The user's organization (tenant) id. <c>null</c> means a host user.
    /// This is the source of the <c>ensa:tenantId</c> claim in the token.
    /// </summary>
    public static int? GetTenantId(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.TenantId;
    }

    /// <summary>Display name shown on screen; falls back to the user name when empty.</summary>
    public static string GetDisplayName(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var fullName = user.FullName;
        return string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;
    }
}
