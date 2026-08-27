using System.Collections.Immutable;
using System.Globalization;
using System.Security.Claims;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Membership;
using Ensa.Domain.Common;
using Ensa.Domain.Membership;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Ensa.HttpApi.Host.Controllers;

/// <summary>
/// The OpenIddict token and userinfo endpoints.
/// <para>
/// Supported flows:
/// <list type="bullet">
/// <item><c>password</c> — SPA login (username + password).</item>
/// <item><c>refresh_token</c> — silent renewal.</item>
/// <item><c>client_credentials</c> — service-to-service calls (no user context).</item>
/// </list>
/// </para>
/// <para>
/// The Ensa-specific claims carried by the issued access token:
/// <c>ensa:tenantId</c> (single) and <c>ensa:permission</c> (multiple).
/// </para>
/// </summary>
[ApiController]
[Produces("application/json")]
public sealed class AuthorizationController(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IPermissionResolver permissionResolver,
    IUserRepository userRepository,
    IDataFilter dataFilter,
    ICurrentTenant currentTenant,
    ILogger<AuthorizationController> logger) : ControllerBase
{
    /// <summary>
    /// Whether this account may be issued a token.
    /// <para>
    /// The answer moved off the user row: an account being usable is recorded in
    /// <c>UserProfile</c>, and a user with no profile at all is treated as unusable rather than
    /// waved through — a half-created account should not be able to sign in.
    /// </para>
    /// </summary>
    private async Task<bool> CanSignInAsync(User user)
    {
        var facts = await userRepository.GetAuthorizationFactsAsync(user.Id);

        return facts is { } who && who.CanSignIn();
    }

    /// <summary>
    /// Resolves the user with the tenant filter temporarily switched off.
    /// <para>
    /// <b>Why this is needed:</b> <c>User</c> implements <c>IMultiTenant</c> and the global query
    /// filter reads <c>TenantId == CurrentTenant.Id || TenantId == null</c>. No token has been
    /// issued yet at login time, so <c>CurrentTenant.Id</c> is <c>null</c>; with the filter on,
    /// only host users can be found and <b>no tenant-bound user is able to log in</b>. The tenant
    /// id is read from the record that is found anyway.
    /// </para>
    /// <para>
    /// The filter is off only for the duration of this one lookup; once the user is
    /// authenticated the tenant context is established through the <c>ensa:tenantId</c> claim.
    /// </para>
    /// </summary>
    private async Task<User?> UserSolveAsync(Func<Task<User?>> search)
    {
        using (dataFilter.Disable<IMultiTenant>())
        {
            return await search();
        }
    }

    /// <summary>
    /// The authentication type used for the ClaimsIdentity that OpenIddict treats as
    /// authenticated. It must not be empty.
    /// </summary>
    private const string AuthenticationType = "OpenIddict";

    private const string SecurityStampClaimType = "AspNet.Identity.SecurityStamp";

    // =====================================================================
    //  POST /connect/token
    // =====================================================================

    /// <summary>The token endpoint. Dispatches to the matching flow based on the grant type.</summary>
    /// <remarks>
    /// <para><b>password:</b></para>
    /// <code>
    /// POST /connect/token
    /// Content-Type: application/x-www-form-urlencoded
    ///
    /// grant_type=password&amp;username=admin&amp;password=Ensa!2026&amp;scope=openid profile email roles offline_access ensa
    /// </code>
    /// <para><b>refresh_token:</b></para>
    /// <code>
    /// grant_type=refresh_token&amp;refresh_token=...
    /// </code>
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
                      ?? throw new InvalidOperationException(
                          "The OpenIddict server request could not be resolved. Check the middleware order.");

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordGrantAsync(request, cancellationToken);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenGrantAsync(cancellationToken);
        }

        if (request.IsClientCredentialsGrantType())
        {
            return HandleClientCredentialsGrant(request);
        }

        return AuthError(
            OpenIddictConstants.Errors.UnsupportedGrantType,
            $"The '{request.GrantType}' flow is not supported.");
    }

    // --------------------------------------------------------- password

    private async Task<IActionResult> HandlePasswordGrantAsync(
        OpenIddictRequest request,
        CancellationToken cancellationToken)
    {
        // Sign in with either the username or the e-mail address
        var userName = request.Username ?? string.Empty;

        var user = await UserSolveAsync(async () =>
            await userManager.FindByNameAsync(userName)
            ?? (userName.Contains('@', StringComparison.Ordinal)
                ? await userManager.FindByEmailAsync(userName)
                : null));

        if (user is null)
        {
            logger.LogWarning("Failed sign-in attempt: user not found. UserName={UserName}", userName);
            return InvalidCredentials();
        }

        // No token is issued to a passive or deleted user. Whether an account may be used lives
        // in UserProfile now, so the repository is asked rather than the row.
        if (!await CanSignInAsync(user))
        {
            logger.LogWarning("A passive or deleted user attempted to sign in. UserId={UserId}", user.Id);
            return AuthError(
                OpenIddictConstants.Errors.InvalidGrant,
                "Your account is not active. Please contact your system administrator.");
        }

        var result = await signInManager.CheckPasswordSignInAsync(
            user, request.Password ?? string.Empty, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            logger.LogWarning("Account is locked out. UserId={UserId}, LockoutEnd={LockoutEnd}", user.Id, user.LockoutEnd);
            return AuthError(
                OpenIddictConstants.Errors.InvalidGrant,
                "Your account has been temporarily locked after too many failed attempts. "
                + "Please try again in 15 minutes.");
        }

        if (result.IsNotAllowed)
        {
            return AuthError(
                OpenIddictConstants.Errors.InvalidGrant,
                "Your account is not allowed to sign in.");
        }

        if (result.RequiresTwoFactor)
        {
            return AuthError(
                OpenIddictConstants.Errors.InvalidGrant,
                "Two-factor authentication is required.");
        }

        if (!result.Succeeded)
        {
            logger.LogWarning("Incorrect password. UserId={UserId}", user.Id);
            return InvalidCredentials();
        }

        var principal = await CreatePrincipalAsync(user, request.GetScopes(), cancellationToken);

        logger.LogInformation(
            "Sign-in succeeded. UserId={UserId}, Tenant={TenantId}", user.Id, user.GetTenantId());

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // ----------------------------------------------------- refresh_token

    private async Task<IActionResult> HandleRefreshTokenGrantAsync(CancellationToken cancellationToken)
    {
        // OpenIddict validates the refresh token and carries the principal inside it over to here.
        var authenticateResult = await HttpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            return AuthError(
                OpenIddictConstants.Errors.InvalidGrant,
                "The refresh token is invalid or has expired.");
        }

        var subject = authenticateResult.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrWhiteSpace(subject))
        {
            return AuthError(
                OpenIddictConstants.Errors.InvalidGrant,
                "The refresh token is not bound to a user.");
        }

        var user = await UserSolveAsync(() => userManager.FindByIdAsync(subject));
        if (user is null)
        {
            return AuthError(
                OpenIddictConstants.Errors.InvalidGrant,
                "The user the refresh token belongs to could not be found.");
        }

        // The account may have been deactivated since the token was issued.
        if (!await CanSignInAsync(user))
        {
            logger.LogWarning("A passive user attempted to renew a token. UserId={UserId}", user.Id);
            return AuthError(
                OpenIddictConstants.Errors.InvalidGrant,
                "Your account is not active. Please sign in again.");
        }

        if (!await signInManager.CanSignInAsync(user))
        {
            return AuthError(
                OpenIddictConstants.Errors.InvalidGrant,
                "Your account is not allowed to sign in.");
        }

        // Refuse the renewal if the security stamp changed, e.g. after a password change.
        var stamp = authenticateResult.Principal.GetClaim(SecurityStampClaimType);
        if (!string.IsNullOrEmpty(stamp))
        {
            var currentStamp = await userManager.GetSecurityStampAsync(user);
            if (!string.Equals(stamp, currentStamp, StringComparison.Ordinal))
            {
                logger.LogInformation(
                    "Refresh token rejected because the security stamp changed. UserId={UserId}", user.Id);
                return AuthError(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "Your session details have changed. Please sign in again.");
            }
        }

        // Roles and permissions may have changed, so the principal is rebuilt on every renewal.
        var principal = await CreatePrincipalAsync(
            user, authenticateResult.Principal.GetScopes(), cancellationToken);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // ------------------------------------------------ client_credentials

    /// <summary>
    /// The service-to-service flow. There is no user context; the permissions come from the
    /// scope/permission definitions on the client's own OpenIddict records.
    /// </summary>
    private IActionResult HandleClientCredentialsGrant(OpenIddictRequest request)
    {
        var clientId = request.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return AuthError(
                OpenIddictConstants.Errors.InvalidClient,
                "client_id is required for the client_credentials flow.");
        }

        var identity = new ClaimsIdentity(
            AuthenticationType,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, clientId));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, clientId));

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetDestinations(GetDestinations);

        logger.LogInformation("client_credentials token issued. ClientId={ClientId}", clientId);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // =====================================================================
    //  GET|POST /connect/userinfo
    // =====================================================================

    /// <summary>
    /// Returns the basic details of the access token's owner:
    /// <c>sub</c>, <c>name</c>, <c>email</c>, <c>ensa:tenantId</c>, <c>role</c>.
    /// </summary>
    [Authorize(AuthenticationSchemes = OpenIddict.Validation.AspNetCore
        .OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> UserInfo()
    {
        var subject = User.GetClaim(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrWhiteSpace(subject))
        {
            return AuthError(
                OpenIddictConstants.Errors.InvalidToken,
                "The token is not bound to a user.");
        }

        var user = await UserSolveAsync(() => userManager.FindByIdAsync(subject));
        if (user is null || !await CanSignInAsync(user))
        {
            return AuthError(
                OpenIddictConstants.Errors.InvalidToken,
                "The user the token belongs to is no longer valid.");
        }

        var roles = await userManager.GetRolesAsync(user);

        var claims = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [OpenIddictConstants.Claims.Subject] = user.Id.ToString(CultureInfo.InvariantCulture),
            [OpenIddictConstants.Claims.Name] = user.UserName,
            [OpenIddictConstants.Claims.PreferredUsername] = user.UserName,
            [OpenIddictConstants.Claims.Email] = user.Email,
            [OpenIddictConstants.Claims.EmailVerified] = user.EmailConfirmed,
            [EnsaClaimTypes.TenantId] = user.GetTenantId(),
            [OpenIddictConstants.Claims.Role] = roles.ToArray()
        };

        return Ok(claims);
    }

    // =====================================================================
    //  Helpers
    // =====================================================================

    /// <summary>
    /// Builds the <see cref="ClaimsPrincipal"/> that is written into the user's access and refresh tokens.
    /// <para>
    /// The permissions are resolved through <see cref="IPermissionResolver"/>, so the contents of
    /// the token and the result of <c>IAccountAppService.GetPermissionsAsync</c> always come from
    /// the same source.
    /// </para>
    /// <para>
    /// The whole method runs inside the signed-in user's own tenant context. Nothing has resolved
    /// a tenant yet at this point in the request - the token that would carry it is what is being
    /// built - so without the switch the ambient tenant is the host, the tenant-scoped rows behind
    /// the user's roles and permissions fall outside the global query filter, and the token comes
    /// out with no permission claims at all. The user then signs in successfully and is refused by
    /// every endpoint, which is indistinguishable from having been granted nothing.
    /// </para>
    /// </summary>
    private async Task<ClaimsPrincipal> CreatePrincipalAsync(
        User user,
        ImmutableArray<string> scopes,
        CancellationToken cancellationToken)
    {
        using var tenantScope = currentTenant.Change(user.GetTenantId());

        var identity = new ClaimsIdentity(
            AuthenticationType,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        identity.AddClaim(new Claim(
            OpenIddictConstants.Claims.Subject, user.Id.ToString(CultureInfo.InvariantCulture)));

        if (!string.IsNullOrWhiteSpace(user.UserName))
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, user.UserName));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.PreferredUsername, user.UserName));
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, user.Email));
        }

        // The tenant claim — ICurrentTenant and TenantResolutionMiddleware read this.
        var tenantId = user.GetTenantId();
        if (tenantId.HasValue)
        {
            identity.AddClaim(new Claim(
                EnsaClaimTypes.TenantId, tenantId.Value.ToString(CultureInfo.InvariantCulture)));
        }

        // The company claim - present only for a user who belongs to one client workplace. It is
        // what narrows every company-scoped query to that workplace, so it is read from the
        // stored record and can never be supplied by the caller.
        var facts = await userRepository.GetAuthorizationFactsAsync(user.Id);

        if (facts?.CompanyId is { } companyId)
        {
            identity.AddClaim(new Claim(
                EnsaClaimTypes.CompanyId, companyId.ToString(CultureInfo.InvariantCulture)));
        }

        // Roles (multiple). The legacy bool flags are turned into role claims too, so that
        // TenantResolutionMiddleware and the policies all look at one source: the role claim.
        // Straight from Identity. The three administrator booleans that used to be folded in
        // here are role assignments now, so UserRole already carries them and adding them again
        // would mean two sources for one answer -- which is what this comment used to explain
        // away.
        var roles = new HashSet<string>(await userManager.GetRolesAsync(user), StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role));
        }

        // Business permissions are deliberately NOT written into the token. They are the
        // application's own authorization model, evaluated per request against the permission
        // tables, and a token that carried them would be a copy that goes stale the moment an
        // administrator changes what someone may do — for as long as the token lives. The client
        // asks /api/account/permissions for what it should show; the server never trusts that
        // answer, it re-evaluates.

        // Security stamp: lets a password change invalidate the older refresh tokens.
        identity.AddClaim(new Claim(SecurityStampClaimType, await userManager.GetSecurityStampAsync(user)));

        var principal = new ClaimsPrincipal(identity);

        principal.SetScopes(scopes);
        principal.SetDestinations(GetDestinations);

        return principal;
    }

    /// <summary>
    /// Decides which token each claim is written into.
    /// <para>
    /// The Ensa-specific claims (<c>ensa:tenantId</c>, <c>ensa:permission</c>) are written into
    /// the <b>access_token</b> only, so they do not bloat the id_token for nothing.
    /// <c>SecurityStamp</c> is written into no token at all — it travels only inside the refresh
    /// token's encrypted payload.
    /// </para>
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Subject:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case OpenIddictConstants.Claims.Name:
            case OpenIddictConstants.Claims.PreferredUsername:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claim.Subject?.HasScope(OpenIddictConstants.Scopes.Profile) == true)
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }

                yield break;

            case OpenIddictConstants.Claims.Email:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claim.Subject?.HasScope(OpenIddictConstants.Scopes.Email) == true)
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }

                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claim.Subject?.HasScope(OpenIddictConstants.Scopes.Roles) == true)
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }

                yield break;

            case EnsaClaimTypes.TenantId:
            case EnsaClaimTypes.CompanyId:
            case EnsaClaimTypes.Permission:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;

            // Written into no token.
            case SecurityStampClaimType:
                yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }

    /// <summary>One uniform response that does not tell a wrong username from a wrong password (blocks user enumeration).</summary>
    private IActionResult InvalidCredentials() => AuthError(
        OpenIddictConstants.Errors.InvalidGrant,
        "The user name or password is incorrect.");

    /// <summary>
    /// Makes OpenIddict emit the standard OAuth error body:
    /// <c>{ "error": "...", "error_description": "..." }</c>
    /// </summary>
    private IActionResult AuthError(string error, string description)
    {
        var properties = new AuthenticationProperties(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        });

        return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
