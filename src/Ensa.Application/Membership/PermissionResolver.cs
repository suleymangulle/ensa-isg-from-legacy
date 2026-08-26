using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Membership;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Membership;

/// <summary>
/// Resolves a user's effective permission set from the union of three sources.
/// <para>
/// Sources:
/// <list type="number">
/// <item><b>System administrator shortcut</b> — when <c>User.SystemAdministrator</c>
/// (legacy <c>SerAdmin</c>) is set, <see cref="EnsaPermissions.GetAll"/> is returned and no further
/// check is performed.</item>
/// <item><b>Legacy permission model</b> — <see cref="IPermissionManager"/>. Package and organization
/// type gates, explicit denials and user type restrictions are applied there; the result is a set of
/// <c>Permission.PermissionTarget</c> names that match the <c>EnsaPermissions</c> constants.</item>
/// <item><b>Role and user claims</b> — the <c>ensa:permission</c> rows in <c>AspNetRoleClaims</c> and
/// <c>AspNetUserClaims</c>.</item>
/// </list>
/// </para>
/// <para>
/// When <see cref="IPermissionManager"/> is not registered in DI (for example while the permission
/// tables have not been seeded yet) it is skipped silently and only claim-based permissions are
/// used, so the authentication flow never breaks.
/// </para>
/// </summary>
public sealed class PermissionResolver(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IServiceProvider serviceProvider,
    ILogger<PermissionResolver> logger) : IPermissionResolver
{
    public async Task<IReadOnlyList<string>> GetPermissionsAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();

        // 1) System administrator: every permission.
        if (user.IsHostAdmin())
        {
            return [.. EnsaPermissions.GetAll()];
        }

        var permissions = new HashSet<string>(StringComparer.Ordinal);

        // 2) Legacy Yetki_T / KullaniciYetki_T model, when it is available.
        await AddLegacyPermissionsAsync(user, permissions, cancellationToken);

        // 3) Role claims.
        foreach (var roleName in await userManager.GetRolesAsync(user))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                logger.LogWarning("The user's '{Role}' role was not found in the role table.", roleName);
                continue;
            }

            AddPermissionClaims(await roleManager.GetClaimsAsync(role), permissions);
        }

        // 4) User-specific claims.
        AddPermissionClaims(await userManager.GetClaimsAsync(user), permissions);

        return [.. permissions.OrderBy(x => x, StringComparer.Ordinal)];
    }

    private async Task AddLegacyPermissionsAsync(
        User user,
        HashSet<string> permissions,
        CancellationToken cancellationToken)
    {
        try
        {
            // NOT a hard dependency: the sign-in flow must work even before the permission tables
            // and repositories are wired up. Resolving the service can itself throw (a missing
            // repository, for instance), which is why the resolve call is inside the try as well.
            if (serviceProvider.GetService(typeof(IPermissionManager)) is not IPermissionManager permissionManager)
            {
                return;
            }

            var targets = await permissionManager.GetPermissionTargetsAsync(user.Id, cancellationToken);
            permissions.UnionWith(targets);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A failing permission lookup would lock the user out entirely, so the error is
            // swallowed, logged, and resolution continues with the claim-based permissions.
            logger.LogError(
                exception,
                "Legacy permission resolution failed; only claim-based permissions will be used. UserId={UserId}",
                user.Id);
        }
    }

    private static void AddPermissionClaims(
        IEnumerable<System.Security.Claims.Claim> claims,
        HashSet<string> permissions)
    {
        foreach (var claim in claims)
        {
            if (string.Equals(claim.Type, EnsaClaimTypes.Permission, StringComparison.Ordinal))
            {
                permissions.Add(claim.Value);
            }
        }
    }
}
