using System.Globalization;
using System.Security.Claims;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Common;
using Ensa.Domain.Membership;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
    /// Whether the signed-in user holds a permission.
    /// <para>
    /// This asks <see cref="IPermissionManager"/>, the same authority the endpoint authorization
    /// asks, so the controller layer and the application layer cannot reach different answers.
    /// It deliberately does not read a claim: business permissions are not in the token, because a
    /// token that carried them would keep answering with what was true when it was issued.
    /// </para>
    /// <para>
    /// Synchronous because <c>ICurrentUser</c> is, and the call sites are few. The permission set
    /// is resolved once per request and held, so several checks in one request cost one query.
    /// </para>
    /// </summary>
    public bool HasPermission(string permissionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);

        if (Id is not { } userId)
        {
            return false;
        }

        var targets = _permissionTargets ??= ResolveTargets(userId);

        return targets.Contains(permissionName);
    }

    private HashSet<string>? _permissionTargets;

    private HashSet<string> ResolveTargets(int userId)
    {
        var manager = httpContextAccessor.HttpContext?.RequestServices.GetService<IPermissionManager>();

        if (manager is null)
        {
            return [];
        }

        var targets = manager.GetPermissionTargetsAsync(userId).GetAwaiter().GetResult();

        return new HashSet<string>(targets, StringComparer.Ordinal);
    }

    private string? FindFirst(string claimType) => Principal?.FindFirst(claimType)?.Value;

    private static int? ParseInt(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
}
