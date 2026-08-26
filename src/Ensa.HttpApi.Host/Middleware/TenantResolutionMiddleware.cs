using System.Globalization;
using System.Security.Claims;
using Ensa.Application.Contracts.Permissions;
using Ensa.EntityFrameworkCore.Ambient;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;

namespace Ensa.HttpApi.Host.Middleware;

/// <summary>
/// Establishes the active tenant context for each request. It writes the value into
/// <see cref="ICurrentTenantAccessor"/>; the global query filter of <c>EnsaDbContext</c>
/// reads it back through <c>ICurrentTenant</c>.
/// <para>
/// Resolution order:
/// <list type="number">
/// <item>The <c>ensa:tenantId</c> claim on the access token (the normal path).</item>
/// <item>The <c>X-Ensa-TenantId</c> header — an override available <b>only</b> to host
/// administrators in the <c>SystemAdministrator</c> role. Sending the header empty falls back
/// to the host context (the records shared by all tenants).</item>
/// </list>
/// </para>
/// <para>
/// <b>Ordering is critical:</b> it must be registered <b>after</b> <c>UseAuthentication()</c>
/// and <b>before</b> <c>UseAuthorization()</c>; otherwise <c>HttpContext.User</c> has not been
/// populated yet.
/// </para>
/// </summary>
public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    ILogger<TenantResolutionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ICurrentTenantAccessor tenantAccessor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenantAccessor);

        var previous = tenantAccessor.Current;
        tenantAccessor.Current = Resolve(context);

        try
        {
            await next(context);
        }
        finally
        {
            // Even though this is AsyncLocal, we deliberately restore the previous context
            // when the request ends.
            tenantAccessor.Current = previous;
        }
    }

    private TenantInfo? Resolve(HttpContext context)
    {
        var user = context.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var tokenTenantId = ParseInt(user.FindFirst(EnsaClaimTypes.TenantId)?.Value);
        var fromToken = tokenTenantId.HasValue ? new TenantInfo(tokenTenantId, null) : null;

        if (!IsHostAdmin(user))
        {
            return fromToken;
        }

        if (!context.Request.Headers.TryGetValue(EnsaHttpHeaders.TenantId, out var headerValues))
        {
            return fromToken;
        }

        var raw = headerValues.ToString();

        if (string.IsNullOrWhiteSpace(raw))
        {
            // An empty header means the host context was asked for deliberately.
            logger.LogDebug("Host administrator switched to the host context. UserId={UserId}", GetSubject(user));
            return null;
        }

        var overridden = ParseInt(raw);
        if (overridden is null)
        {
            logger.LogWarning(
                "Ignored an invalid {Header} header: '{Value}'", EnsaHttpHeaders.TenantId, raw);
            return fromToken;
        }

        logger.LogInformation(
            "Host administrator switched the tenant context. UserId={UserId}, TenantId={TenantId}",
            GetSubject(user), overridden);

        return new TenantInfo(overridden, null);
    }

    /// <summary>
    /// Scans the claims directly rather than calling <c>ClaimsPrincipal.IsInRole</c>, so the
    /// answer stays correct no matter how the identity's <c>RoleClaimType</c> is configured.
    /// </summary>
    private static bool IsHostAdmin(ClaimsPrincipal user)
    {
        foreach (var claim in user.Claims)
        {
            if ((claim.Type == OpenIddictConstants.Claims.Role || claim.Type == ClaimTypes.Role)
                && string.Equals(claim.Value, EnsaRoles.SystemAdministrator, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetSubject(ClaimsPrincipal user)
        => user.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;

    private static int? ParseInt(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
}
