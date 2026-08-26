using System.Globalization;
using System.Security.Claims;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Common;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;

namespace Ensa.HttpApi.Host.Ambient;

/// <summary>
/// <see cref="ICurrentUser"/> implementation — it reads its values from the access token's claims.
/// <para>
/// Claim mapping:
/// <list type="bullet">
/// <item><c>sub</c> → <see cref="Id"/></item>
/// <item><c>name</c> / <c>preferred_username</c> → <see cref="UserName"/></item>
/// <item><c>email</c> → <see cref="Email"/></item>
/// <item><c>ensa:tenantId</c> → <see cref="TenantId"/></item>
/// <item><c>role</c> (multiple) → <see cref="Roles"/></item>
/// <item><c>ensa:permission</c> (multiple) → <see cref="HasPermission"/></item>
/// </list>
/// </para>
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public int? Id => ParseInt(
        FindFirst(OpenIddictConstants.Claims.Subject) ?? FindFirst(ClaimTypes.NameIdentifier));

    public string? UserName =>
        FindFirst(OpenIddictConstants.Claims.Name)
        ?? FindFirst(OpenIddictConstants.Claims.PreferredUsername)
        ?? FindFirst(ClaimTypes.Name);

    public string? Email =>
        FindFirst(OpenIddictConstants.Claims.Email) ?? FindFirst(ClaimTypes.Email);

    public int? TenantId => ParseInt(FindFirst(EnsaClaimTypes.TenantId));

    public int? CompanyId => ParseInt(FindFirst(EnsaClaimTypes.CompanyId));

    public string[] Roles
    {
        get
        {
            var principal = Principal;
            if (principal is null)
            {
                return [];
            }

            return [.. principal.Claims
                .Where(c => c.Type == OpenIddictConstants.Claims.Role || c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)];
        }
    }

    public bool IsInRole(string roleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
        return Array.Exists(Roles, r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The permission check looks at the <c>ensa:permission</c> claims on the token.
    /// <para>
    /// This reads the <b>exact same</b> source as the <c>[Authorize(Policy = ...)]</c> policies,
    /// so the controller and app service layers can never reach different decisions.
    /// For the <c>SystemAdministrator</c> role the permissions are expanded while the token is
    /// issued (see <c>PermissionResolver</c>); there is no extra shortcut here.
    /// </para>
    /// </summary>
    public bool HasPermission(string permissionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);

        var principal = Principal;
        if (principal is null)
        {
            return false;
        }

        foreach (var claim in principal.Claims)
        {
            if (string.Equals(claim.Type, EnsaClaimTypes.Permission, StringComparison.Ordinal)
                && string.Equals(claim.Value, permissionName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private string? FindFirst(string claimType) => Principal?.FindFirst(claimType)?.Value;

    private static int? ParseInt(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
}
