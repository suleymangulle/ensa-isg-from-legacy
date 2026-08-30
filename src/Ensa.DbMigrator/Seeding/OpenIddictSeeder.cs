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
/// client, which is what this registers — a secret here would be security theatre. The mobile
/// application is public for the same reason: a secret shipped inside an installable package is
/// readable by anyone who unpacks it.
/// </para>
/// <para>
/// <b>Two clients, not one.</b> The web and mobile applications are the same product against the
/// same API and could have shared a client id. They do not, because a client id is the only thing
/// that distinguishes them afterwards: revoking the mobile application's tokens, or reading which
/// of the two a session came from, is impossible once both call themselves <c>ensa-spa</c>. The
/// grants and scopes they may ask for are identical today; the ids are what allow them to stop
/// being identical without a migration.
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

    /// <summary>The first-party React Native application (repository: ensa-isg-mobile).</summary>
    public const string MobileClientId = "ensa-mobile";

    /// <summary>The resource name a token for this API carries.</summary>
    private const string ApiResource = "ensa-api";

    public int Order => 90;

    public string Name => "OpenIddict client and scope registration";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedApiScopeAsync(cancellationToken);
        await SeedClientAsync(SpaClientId, "Ensa web application", cancellationToken);
        await SeedClientAsync(MobileClientId, "Ensa mobile application", cancellationToken);
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

    /// <summary>
    /// Registers one first-party public client.
    /// </summary>
    /// <remarks>
    /// Both clients are declared with the same permissions from one place rather than from two
    /// near-identical methods: the day a grant is added or withdrawn, it has to happen for both,
    /// and a copy is how one of them gets forgotten.
    /// </remarks>
    private async Task SeedClientAsync(
        string clientId,
        string displayName,
        CancellationToken cancellationToken)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = displayName,
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

        var existing = await applications.FindByClientIdAsync(clientId, cancellationToken);

        if (existing is null)
        {
            await applications.CreateAsync(descriptor, cancellationToken);
            logger.LogInformation("  client {ClientId} registered", clientId);
            return;
        }

        await applications.UpdateAsync(existing, descriptor, cancellationToken);
        logger.LogInformation("  client {ClientId} already registered, refreshed", clientId);
    }
}
