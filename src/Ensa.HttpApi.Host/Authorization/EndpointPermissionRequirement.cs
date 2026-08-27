using Ensa.Domain.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Ensa.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Ensa.HttpApi.Host.Authorization;

/// <summary>
/// The single requirement every authenticated endpoint carries. It holds nothing: which permission
/// applies is not known here, and that is the point.
/// </summary>
public sealed class EndpointPermissionRequirement : IAuthorizationRequirement;

/// <summary>
/// Decides whether the caller may reach the endpoint they asked for.
/// <para>
/// <b>How the legacy application did it.</b> <c>PermissionCheck.Authorize</c> asked the runtime
/// which method it had been called from, looked that name up in <c>Yetki_T.YetkiHedefi</c>, and
/// evaluated the four gates against the row it found. No controller in that codebase named a
/// permission; the permission table said which code it guarded.
/// </para>
/// <para>
/// <b>How this does it.</b> The same shape, with routing metadata in place of a stack walk:
/// ASP.NET Core already knows which controller and action it is about to run, so the endpoint is
/// identified from <see cref="ControllerActionDescriptor"/>, looked up in
/// <see cref="PermissionEndpoint"/>, and handed to <see cref="IPermissionManager"/> — which is the
/// faithful port of the legacy four-gate query and remains the authority on the answer.
/// </para>
/// <para>
/// <b>An unmapped endpoint is refused.</b> Not out of caution — because that is what the legacy
/// application did when no <c>Yetki_T</c> row matched: <i>"Bu eylem henüz kullanıma açılmamış
/// yada kullanımdan kaldırılmıştır"</i>. An endpoint nobody has decided about is an endpoint
/// nobody has authorised.
/// </para>
/// <para>
/// The map is small — a few hundred rows that change only when the application is deployed — so it
/// is read once and kept. Permissions themselves are never cached: they are per user, per tenant,
/// and change while a session is live.
/// </para>
/// </summary>
public sealed class EndpointPermissionHandler(
    IPermissionManager permissionManager,
    IEndpointPermissionMap map,
    ILogger<EndpointPermissionHandler> logger)
    : AuthorizationHandler<EndpointPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EndpointPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (context.Resource is not HttpContext http)
        {
            // Nothing to identify the endpoint with. Saying "allowed" here would turn every
            // future authorization call that arrives without an HttpContext into a hole.
            logger.LogWarning("Authorization ran without an HttpContext; the endpoint cannot be identified.");
            return;
        }

        var descriptor = http.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (descriptor is null)
        {
            logger.LogWarning("Authorization ran on an endpoint that is not a controller action.");
            return;
        }

        var userId = UserIdOf(context.User);
        if (userId is null)
        {
            return;
        }

        var lookup = await map.FindAsync(descriptor.ControllerName, descriptor.ActionName, http.RequestAborted);

        if (lookup is null)
        {
            logger.LogWarning(
                "{Controller}.{Action} has no row in the endpoint permission map, so it is refused.",
                descriptor.ControllerName, descriptor.ActionName);
            return;
        }

        // A deliberate null: signing in, reading your own profile, fetching the menu. A valid
        // token is the whole requirement.
        if (lookup.PermissionId is not { } permissionId)
        {
            context.Succeed(requirement);
            return;
        }

        var effective = await permissionManager.GetEffectivePermissionIdsAsync(userId.Value, http.RequestAborted);

        if (effective.Contains(permissionId))
        {
            context.Succeed(requirement);
        }
    }

    /// <summary>
    /// The signed-in user's id, read from the same claim the rest of the host reads it from —
    /// OpenIddict writes the subject, and older tokens carry the ASP.NET name identifier.
    /// </summary>
    private static int? UserIdOf(System.Security.Claims.ClaimsPrincipal user)
    {
        var value = user.FindFirst(OpenIddictConstants.Claims.Subject)?.Value
                    ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var id) ? id : null;
    }
}

/// <summary>The endpoint map, read once and kept for the life of the process.</summary>
public interface IEndpointPermissionMap
{
    Task<PermissionEndpoint?> FindAsync(string controller, string action, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IEndpointPermissionMap"/>
public sealed class EndpointPermissionMap(IServiceScopeFactory scopeFactory) : IEndpointPermissionMap
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<(string Controller, string Action), PermissionEndpoint>? _entries;

    public async Task<PermissionEndpoint?> FindAsync(
        string controller,
        string action,
        CancellationToken cancellationToken = default)
    {
        var entries = _entries ?? await LoadAsync(cancellationToken);

        return entries.TryGetValue((controller, action), out var entry) ? entry : null;
    }

    private async Task<Dictionary<(string, string), PermissionEndpoint>> LoadAsync(
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (_entries is not null)
            {
                return _entries;
            }

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EnsaDbContext>();

            var rows = await context.Set<PermissionEndpoint>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return _entries = rows.ToDictionary(x => (x.ControllerName, x.ActionName));
        }
        finally
        {
            _gate.Release();
        }
    }
}
