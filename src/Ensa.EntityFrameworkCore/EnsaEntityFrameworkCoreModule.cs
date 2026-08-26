using Ensa.Domain.Common;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.Ambient;
using Ensa.EntityFrameworkCore.Repositories;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Ensa.EntityFrameworkCore;

/// <summary>
/// DI registration of the EntityFrameworkCore layer (the counterpart of ABP's <c>EnsaEntityFrameworkCoreModule</c>).
/// </summary>
public static class EnsaEntityFrameworkCoreModule
{
    /// <summary>Name of the migration history table.</summary>
    public const string MigrationsHistoryTable = "__EnsaMigrationsHistory";

    /// <summary>
    /// Registers <see cref="EnsaDbContext"/>, the repositories, the ambient services and the encryption
    /// options with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// Configuration containing the <c>ConnectionStrings:Default</c> and <c>Encryption</c> sections.
    /// </param>
    public static IServiceCollection AddEnsaEntityFrameworkCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddDbContext(services, configuration);
        AddAmbientServices(services);
        AddGenericRepositories(services);
        AddModuleRepositories(services);
        AddDomainPolicyProviders(services);
        AddEncryption(services, configuration);

        return services;
    }

    // ------------------------------------------------------------------

    private static void AddDbContext(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // We fail fast instead of silently falling back to localdb: connecting to the wrong
            // database is a far more expensive mistake than not connecting at all.
            throw new InvalidOperationException(
                "The 'ConnectionStrings:Default' configuration was not found. " +
                "Define it in appsettings.json or through the 'ConnectionStrings__Default' environment variable.");
        }

        services.AddDbContext<EnsaDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable(MigrationsHistoryTable, EnsaDomainSharedConsts.DbSchema)));
    }

    /// <summary>
    /// Clock, data filter, tenant and user context.
    /// <para>
    /// All of them are added with <c>TryAdd</c>: the Host layer (with real implementations that resolve the
    /// tenant/user from the HTTP context) can replace these registrations with its own.
    /// </para>
    /// </summary>
    private static void AddAmbientServices(IServiceCollection services)
    {
        // They are AsyncLocal-based, so registering them as singletons is both safe and correct;
        // the state lives in the call flow (async context), not in the instance.
        services.TryAddSingleton<IClock, Clock>();
        services.TryAddSingleton<IDataFilter, DataFilter>();
        services.TryAddSingleton<ICurrentTenantAccessor, AsyncLocalCurrentTenantAccessor>();

        services.TryAddScoped<ICurrentTenant, CurrentTenant>();

        // The real user context is registered in the Host layer; here we only leave a safe
        // default for background jobs and tests.
        services.TryAddScoped<ICurrentUser>(_ => NullCurrentUser.Instance);

        services.TryAddScoped<IUnitOfWork, UnitOfWork>();
    }

    /// <summary>Open generic repository registrations — no separate registration is needed per entity.</summary>
    private static void AddGenericRepositories(IServiceCollection services)
    {
        services.TryAddScoped(typeof(IRepository<,>), typeof(EfCoreRepository<,>));
        services.TryAddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        services.TryAddScoped(typeof(IReadOnlyRepository<,>), typeof(EfCoreReadOnlyRepository<,>));
        services.TryAddScoped(typeof(IReadOnlyRepository<>), typeof(EfCoreReadOnlyRepository<>));
    }

    /// <summary>
    /// Automatically registers the module-specific repositories (e.g. <c>CompanyRepository</c> →
    /// <c>ICompanyRepository</c>) by scanning the assembly.
    /// <para>
    /// <b>Why scanning?</b> Seven developers work in parallel. Adding a line to this file by hand for every
    /// new repository would create a single merge conflict hotspot and a step that is constantly forgotten.
    /// The rule is: if you write a <b>non-generic</b> interface deriving from <c>IRepository</c>, the class
    /// implementing it is registered automatically.
    /// </para>
    /// <para>
    /// Only non-generic interfaces are registered; closed generics such as <c>IRepository&lt;Company&gt;</c>
    /// are already covered by the open generic registrations.
    /// </para>
    /// </summary>
    private static void AddModuleRepositories(IServiceCollection services)
    {
        var assembly = typeof(EnsaEntityFrameworkCoreModule).Assembly;

        var implementations = assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                           && typeof(IRepository).IsAssignableFrom(type));

        foreach (var implementation in implementations)
        {
            var serviceTypes = implementation
                .GetInterfaces()
                .Where(@interface => typeof(IRepository).IsAssignableFrom(@interface)
                                     && @interface != typeof(IRepository)
                                     && !@interface.IsGenericType);

            foreach (var serviceType in serviceTypes)
            {
                services.TryAddScoped(serviceType, implementation);
            }
        }
    }

    /// <summary>
    /// Registers the policy providers that the domain abstracts in order to stay independent of persistence.
    /// <para>
    /// Domain services (such as <c>CompanyManager</c>) define interfaces like <c>ITenantLimitProvider</c> and
    /// <c>INaceHazardClassProvider</c> so that they do not depend directly on entities of other modules such as
    /// <c>Organization</c> or <c>OccupationCode</c>. Because those interfaces do not derive from
    /// <see cref="IRepository"/>, the repository scan does not pick them up.
    /// </para>
    /// <para>
    /// The rule: a concrete class in <c>Ensa.EntityFrameworkCore</c> that implements an interface declared in the
    /// <c>Ensa.Domain</c> assembly is registered against that interface. This way adding a new provider does not
    /// require changing this file.
    /// </para>
    /// </summary>
    private static void AddDomainPolicyProviders(IServiceCollection services)
    {
        var efAssembly = typeof(EnsaEntityFrameworkCoreModule).Assembly;
        var domainAssembly = typeof(IRepository).Assembly;

        var implementations = efAssembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                           && !typeof(IRepository).IsAssignableFrom(type));

        foreach (var implementation in implementations)
        {
            var serviceTypes = implementation
                .GetInterfaces()
                .Where(@interface => @interface.Assembly == domainAssembly
                                     && !@interface.IsGenericType
                                     && !typeof(IRepository).IsAssignableFrom(@interface)
                                     // IUnitOfWorkTransaction is not a service but a short-lived
                                     // object created by UnitOfWork; it cannot be resolved from DI
                                     // (its constructor expects an open IDbContextTransaction).
                                     && @interface != typeof(IUnitOfWorkTransaction));

            foreach (var serviceType in serviceTypes)
            {
                services.TryAddScoped(serviceType, implementation);
            }
        }
    }

    /// <summary>
    /// Binds the encrypted column options.
    /// <para>
    /// The options are published both as <see cref="IOptions{TOptions}"/> (for runtime consumers) and as the
    /// <see cref="EnsaEncryptionOptions.Current"/> static (for EF model building). Because model building
    /// runs before the DI scope and only once per process, <c>IEntityTypeConfiguration</c> classes need the
    /// static access.
    /// </para>
    /// </summary>
    private static void AddEncryption(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(EnsaEncryptionOptions.SectionName);

        services.Configure<EnsaEncryptionOptions>(section);

        var options = new EnsaEncryptionOptions();
        section.Bind(options);

        // Refuses the development fallback anywhere but Development. See EnsureUsable.
        options.EnsureUsable(configuration["ASPNETCORE_ENVIRONMENT"]
                             ?? configuration["DOTNET_ENVIRONMENT"]
                             ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                             ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"));

        EnsaEncryptionOptions.SetCurrent(options);
    }
}
