using System.Reflection;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Membership;
using Ensa.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ensa.Application;

/// <summary>
/// DI registration for the application layer (the ABP equivalent of
/// <c>EnsaApplicationModule : AbpModule</c>).
/// </summary>
public static class EnsaApplicationModule
{
    /// <summary>
    /// Registers the application layer:
    /// <list type="bullet">
    /// <item>every AutoMapper <c>Profile</c> class in this assembly,</item>
    /// <item>every concrete class implementing <see cref="IApplicationService"/> together with the
    /// <c>I*AppService</c> interfaces it implements (transient).</item>
    /// </list>
    /// <b>Developers never write DI registrations by hand</b> — adding a new
    /// <c>{Module}AppService</c> class is enough.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="autoMapperLicenseKey">
    /// Optional AutoMapper 15+ commercial licence key. It can be passed in from
    /// <c>appsettings.json → AutoMapper:LicenseKey</c>.
    /// </param>
    public static IServiceCollection AddEnsaApplication(
        this IServiceCollection services,
        string? autoMapperLicenseKey = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = typeof(EnsaApplicationModule).Assembly;

        // 1) AutoMapper — every Profile class in this assembly is scanned.
        services.AddAutoMapper(cfg =>
        {
            if (!string.IsNullOrWhiteSpace(autoMapperLicenseKey))
            {
                cfg.LicenseKey = autoMapperLicenseKey;
            }

            cfg.AddMaps(assembly);
        });

        // 2) Identity helper services.
        services.TryAddTransient<IPermissionResolver, PermissionResolver>();

        // 3) Domain services (managers) — discovered by convention in the domain assembly.
        AddDomainServices(services);

        // 4) Convention-based registration of the application services.
        AddApplicationServices(services, assembly);

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IDomainService"/> implementations (the managers) found in
    /// <c>Ensa.Domain</c> as <b>scoped</b> against the <c>I*Manager</c> interfaces they implement.
    /// <para>
    /// Managers are scoped because they depend on repositories. <c>TryAdd</c> is used so that a
    /// future <c>EnsaDomainModule.AddEnsaDomain()</c> would not clash with these registrations.
    /// </para>
    /// </summary>
    private static void AddDomainServices(IServiceCollection services)
    {
        var domainAssembly = typeof(IDomainService).Assembly;

        foreach (var implementation in domainAssembly.GetTypes())
        {
            if (implementation is not { IsClass: true, IsAbstract: false, IsPublic: true }
                || implementation.IsGenericTypeDefinition
                || !typeof(IDomainService).IsAssignableFrom(implementation))
            {
                continue;
            }

            services.TryAddScoped(implementation);

            foreach (var contract in implementation.GetInterfaces())
            {
                if (contract == typeof(IDomainService)
                    || contract.IsGenericTypeDefinition
                    || !typeof(IDomainService).IsAssignableFrom(contract))
                {
                    continue;
                }

                services.TryAddScoped(contract, sp => sp.GetRequiredService(implementation));
            }
        }
    }

    /// <summary>
    /// Registers every concrete class implementing <see cref="IApplicationService"/> as
    /// <b>transient</b>, both against its own type and against its <c>I*AppService</c> interfaces.
    /// </summary>
    private static void AddApplicationServices(IServiceCollection services, Assembly assembly)
    {
        foreach (var implementation in assembly.GetTypes())
        {
            if (implementation is not { IsClass: true, IsAbstract: false, IsPublic: true }
                || implementation.IsGenericTypeDefinition
                || !typeof(IApplicationService).IsAssignableFrom(implementation))
            {
                continue;
            }

            // Register the concrete type once; the interface registrations forward to it.
            services.TryAddTransient(implementation);

            foreach (var contract in implementation.GetInterfaces())
            {
                if (contract == typeof(IApplicationService)
                    || contract.IsGenericTypeDefinition
                    || !typeof(IApplicationService).IsAssignableFrom(contract)
                    || !contract.Name.EndsWith("AppService", StringComparison.Ordinal))
                {
                    continue;
                }

                services.TryAddTransient(contract, sp => sp.GetRequiredService(implementation));
            }
        }
    }
}
