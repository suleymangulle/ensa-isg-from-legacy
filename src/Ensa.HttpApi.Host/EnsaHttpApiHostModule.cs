using Ensa.HttpApi.Host.Compliance;
using Ensa.HttpApi.Host.Mailing;
using Ensa.Domain.Communication;
using Ensa.HttpApi.Host.Storage;
using Ensa.Domain.Documents;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Common;
using Ensa.Domain.Membership;
using Ensa.EntityFrameworkCore;
using Ensa.HttpApi.Host.Ambient;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace Ensa.HttpApi.Host;

/// <summary>
/// DI registration for the host layer: ASP.NET Core Identity, OpenIddict (server + validation),
/// authorization policies, CORS, Swagger and the ambient services.
/// </summary>
public static class EnsaHttpApiHostModule
{
    /// <summary>Name of the CORS policy.</summary>
    public const string CorsPolicyName = "EnsaCors";

    /// <summary>Name of the Swagger document.</summary>
    public const string SwaggerDocumentName = "v1";

    private static readonly string[] DefaultCorsOrigins =
        ["http://localhost:5173", "http://localhost:3000"];

    public static IServiceCollection AddEnsaHttpApiHost(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpContextAccessor();

        AddAmbientServices(services);
        AddEnsaIdentity(services);
        AddEnsaOpenIddict(services, configuration);
        AddEnsaAuthorization(services);
        AddEnsaCors(services, configuration);
        AddEnsaSwagger(services);
        AddDocumentStorage(services, configuration);
        AddMailDelivery(services, configuration);
        AddComplianceSummary(services, configuration);

        return services;
    }

    // ------------------------------------------------------------- Ambient

    /// <summary>
    /// Ambient services that are specific to the HTTP context.
    /// <para>
    /// <c>IClock</c>, <c>ICurrentTenant</c>, <c>ICurrentTenantAccessor</c>, <c>IDataFilter</c>
    /// and <c>IUnitOfWork</c> are already registered by <c>AddEnsaEntityFrameworkCore</c>, so
    /// they are not repeated here.
    /// </para>
    /// <para>
    /// <c>ICurrentUser</c>, by contrast, is registered in the EF layer as <c>NullCurrentUser</c>.
    /// Since <c>TryAdd</c> means "the first registration wins", we <b>replace it explicitly</b>
    /// here: the existing registration is removed first, then the HTTP-based implementation is
    /// added. That way the right service wins regardless of the call order in Program.cs.
    /// </para>
    /// </summary>
    private static void AddAmbientServices(IServiceCollection services)
    {
        services.RemoveAll<ICurrentUser>();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
    }

    // ------------------------------------------------------------ Identity

    /// <summary>
    /// ASP.NET Core Identity.
    /// <para>
    /// Password policy: at least 8 characters, a digit and an uppercase letter required,
    /// a special character optional. Lockout: 15 minutes after 5 failed attempts.
    /// </para>
    /// </summary>
    private static void AddEnsaIdentity(IServiceCollection services)
    {
        services
            .AddIdentity<User, Role>(options =>
            {
                // --- Password policy
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;

                // --- Lockout
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                // --- User
                options.User.RequireUniqueEmail = false;
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;

                // --- Claim compatibility with OpenIddict (short OIDC claim names are used
                //     instead of ASP.NET's default long URIs: sub / name / role / email)
                options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
                options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
                options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
                options.ClaimsIdentity.EmailClaimType = OpenIddictConstants.Claims.Email;
            })
            .AddEntityFrameworkStores<EnsaDbContext>()
            .AddDefaultTokenProviders();

        // AddIdentity makes cookies the default scheme. Because the API works with bearer
        // tokens only, we point the default scheme at the OpenIddict validation handler instead.
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultForbidScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });
    }

    // ----------------------------------------------------------- OpenIddict

    /// <summary>
    /// The OpenIddict server.
    /// <para>
    /// Endpoints: <c>POST /connect/token</c>, <c>GET|POST /connect/userinfo</c>.
    /// Flows: <c>password</c>, <c>refresh_token</c>, <c>client_credentials</c>.
    /// The access_token lives for 1 hour, the refresh_token for 30 days.
    /// </para>
    /// <para>
    /// <c>DisableAccessTokenEncryption()</c>: the access token is issued unencrypted (as a plain
    /// JWT) so that the SPA and Swagger can read its contents.
    /// The signature is still validated.
    /// </para>
    /// </summary>
    private static void AddEnsaOpenIddict(IServiceCollection services, IConfiguration configuration)
    {
        var disableTransportSecurity =
            configuration.GetValue("Ensa:Auth:DisableTransportSecurityRequirement", false);

        services
            .AddOpenIddict()

            .AddCore(options => options
                .UseEntityFrameworkCore()
                .UseDbContext<EnsaDbContext>()
                .ReplaceDefaultEntities<int>())

            .AddServer(options =>
            {
                options
                    .SetTokenEndpointUris("connect/token")
                    .SetUserInfoEndpointUris("connect/userinfo");

                options
                    .AllowPasswordFlow()
                    .AllowRefreshTokenFlow()
                    .AllowClientCredentialsFlow();

                // A registered client is not required (the SPA is a public client).
                options.AcceptAnonymousClients();

                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.Roles,
                    OpenIddictConstants.Scopes.OfflineAccess,
                    EnsaScopes.Api);

                options.SetAccessTokenLifetime(TimeSpan.FromHours(1));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

                // Development certificates. These MUST be swapped for real ones IN PRODUCTION:
                // options.AddEncryptionCertificate(thumbprint).AddSigningCertificate(thumbprint);
                options.AddDevelopmentEncryptionCertificate();
                options.AddDevelopmentSigningCertificate();

                options.DisableAccessTokenEncryption();

                var aspNetCore = options
                    .UseAspNetCore()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough();

                if (disableTransportSecurity)
                {
                    // Development only: lets tokens be obtained over http://localhost.
                    aspNetCore.DisableTransportSecurityRequirement();
                }
            })

            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });
    }

    // ------------------------------------------------------ Authorization

    /// <summary>
    /// Registers one authorization policy under the same name for every permission in
    /// <see cref="EnsaPermissions.GetAll"/>. Controllers consume them by writing
    /// <c>[Authorize(EnsaPermissions.Company.Create)]</c>.
    /// </summary>
    private static void AddEnsaAuthorization(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var permission in EnsaPermissions.GetAll())
            {
                options.AddPolicy(permission, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(EnsaClaimTypes.Permission, permission);
                });
            }

            // An extra policy for the endpoints that require host administrator rights.
            options.AddPolicy(EnsaRoles.SystemAdministrator, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(OpenIddictConstants.Claims.Role, EnsaRoles.SystemAdministrator);
            });
        });
    }

    // --------------------------------------------------------------- CORS

    private static void AddEnsaCors(IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Ensa:Cors:Origins").Get<string[]>();
        if (origins is null || origins.Length == 0)
        {
            origins = DefaultCorsOrigins;
        }

        services.AddCors(options => options.AddPolicy(CorsPolicyName, builder => builder
            .WithOrigins(origins)
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition", EnsaHttpHeaders.CorrelationId)));
    }

    // ------------------------------------------------------------ Swagger

    private static void AddEnsaSwagger(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(SwaggerDocumentName, new OpenApiInfo
            {
                Title = "Ensa API",
                Version = "v1",
                Description =
                    "HTTP API of the Ensa OHS management system. Authentication goes through "
                    + "POST /connect/token using the OpenIddict password and refresh_token flows."
            });

            // DTOs with the same name can live in different namespaces; disambiguate by full name.
            options.CustomSchemaIds(type => type.FullName?.Replace('+', '.') ?? type.Name);

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "OpenIddict access token. Obtain a token from POST /connect/token first, "
                    + "then paste only the token value here."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference("Bearer", document, null!), [] }
            });
        });
    }

    /// <summary>Cultures the API can answer in. The first entry is the default.</summary>
    public static readonly string[] SupportedCultures = ["tr-TR", "en-US"];

    /// <summary>
    /// Request localization: <c>?culture=en-US</c> wins, then the <c>Accept-Language</c> header,
    /// otherwise Turkish. The set is closed — an unsupported culture silently falls back to the
    /// default rather than throwing.
    /// </summary>
    public static RequestLocalizationOptions BuildLocalizationOptions()
    {
        var cultures = SupportedCultures.Select(c => new CultureInfo(c)).ToList();

        var options = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(cultures[0]),
            SupportedCultures = cultures,
            SupportedUICultures = cultures,
            ApplyCurrentCultureToResponseHeaders = true
        };

        // Cookie-based selection is not used; the SPA sends Accept-Language.
        options.RequestCultureProviders =
        [
            new QueryStringRequestCultureProvider { QueryStringKey = "culture", UIQueryStringKey = "culture" },
            new AcceptLanguageHeaderRequestCultureProvider()
        ];

        return options;
    }
    /// <summary>
    /// Registers the document payload store.
    /// <para>
    /// The file system implementation lives in the host rather than in the EF Core project: it
    /// is infrastructure, but not persistence, so swapping in blob storage touches only the
    /// composition root.
    /// </para>
    /// </summary>
    private static void AddDocumentStorage(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DocumentStorageOptions>(
            configuration.GetSection(DocumentStorageOptions.SectionName));

        services.AddSingleton<IDocumentStorage, FileSystemDocumentStorage>();
    }
    /// <summary>
    /// Registers the outgoing mail transport and the background worker that drains the queue.
    /// <para>
    /// The worker is a hosted service rather than something a request triggers: delivery is slow
    /// and needs retrying, and neither belongs inside a database transaction.
    /// </para>
    /// </summary>
    private static void AddMailDelivery(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MailDeliveryOptions>(
            configuration.GetSection(MailDeliveryOptions.SectionName));

        services.AddSingleton<IMailSender, SmtpMailSender>();
        services.AddHostedService<MailDeliveryWorker>();
    }

    /// <summary>
    /// The job that keeps <c>CompanyComplianceSummary</c> current.
    /// <para>
    /// The compliance panel reads a denormalised summary row per company. Nothing wrote those rows
    /// until this job existed, so the panel was permanently empty; the six aggregates behind it are
    /// too heavy to recompute on every screen open and too slow-moving to need it.
    /// </para>
    /// </summary>
    private static void AddComplianceSummary(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ComplianceSummaryOptions>(
            configuration.GetSection(ComplianceSummaryOptions.SectionName));

        services.AddHostedService<ComplianceSummaryWorker>();
    }
}
