using Ensa.Application.Contracts.Permissions;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;

namespace Ensa.DbMigrator.Seeding;

/// <summary>
/// Registers the first-party client and the API scope with OpenIddict.
/// <para>
/// <b>Why this was missing.</b> The server is configured with
/// <c>AcceptAnonymousClients()</c>, so the token endpoint issued tokens to a request that named no
/// client at all — <c>OpenIddictApplications</c> stayed empty while <c>OpenIddictTokens</c> filled
/// up. That works, but it is not how OpenIddict is meant to be used: a first-party application is
/// supposed to be a registered client, so the grants and scopes it may ask for are declared once,
/// in one place, instead of being whatever the request happens to send.
/// </para>
/// <para>
/// <b>Public, not confidential.</b> The SPA runs in a browser, where a client secret would be
/// readable by anyone who opens the developer tools. OpenIddict's answer to that is a public
/// client, which is what this registers — a secret here would be security theatre.
/// </para>
/// <para>
/// <b>Which scopes get a row.</b> Only <c>ensa</c>. <c>openid</c>, <c>profile</c>, <c>email</c>,
/// <c>roles</c> and <c>offline_access</c> are part of the protocol and the server already declares
/// them through <c>RegisterScopes</c>; writing them into the scope table as well would duplicate
/// the framework rather than complete it. The API scope is different — it names a resource, which
/// is what tells the validation handler that a token is meant for this API.
/// </para>
/// <para>
/// Idempotent: an existing client is updated in place rather than duplicated, so the seeder can
/// run on every deployment.
/// </para>
/// </summary>
public sealed class OpenIddictSeeder(
    IOpenIddictApplicationManager applications,
    IOpenIddictScopeManager scopes,
    ILogger<OpenIddictSeeder> logger) : IDataSeeder
{
    /// <summary>The first-party single-page application.</summary>
    public const string SpaClientId = "ensa-spa";

    /// <summary>The resource name a token for this API carries.</summary>
    private const string ApiResource = "ensa-api";

    public int Order => 90;

    public string Name => "OpenIddict client and scope registration";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedApiScopeAsync(cancellationToken);
        await SeedSpaClientAsync(cancellationToken);
    }

    private async Task SeedApiScopeAsync(CancellationToken cancellationToken)
    {
        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = EnsaScopes.Api,
            DisplayName = "Ensa API",
            Resources = { ApiResource },
        };

        var existing = await scopes.FindByNameAsync(EnsaScopes.Api, cancellationToken);

        if (existing is null)
        {
            await scopes.CreateAsync(descriptor, cancellationToken);
            logger.LogInformation("  scope {Scope} registered", EnsaScopes.Api);
            return;
        }

        await scopes.UpdateAsync(existing, descriptor, cancellationToken);
        logger.LogInformation("  scope {Scope} already registered, refreshed", EnsaScopes.Api);
    }

    private async Task SeedSpaClientAsync(CancellationToken cancellationToken)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = SpaClientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "Ensa web application",
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,

                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,

                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                OpenIddictConstants.Permissions.Prefixes.Scope + EnsaScopes.Api,
            },
        };

        var existing = await applications.FindByClientIdAsync(SpaClientId, cancellationToken);

        if (existing is null)
        {
            await applications.CreateAsync(descriptor, cancellationToken);
            logger.LogInformation("  client {ClientId} registered", SpaClientId);
            return;
        }

        await applications.UpdateAsync(existing, descriptor, cancellationToken);
        logger.LogInformation("  client {ClientId} already registered, refreshed", SpaClientId);
    }
}
