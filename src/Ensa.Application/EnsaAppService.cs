using System.Collections.Concurrent;
using System.Globalization;
using AutoMapper;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Common;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ensa.Application;

/// <summary>
/// Base class for every application service (ABP: <c>ApplicationService</c>).
/// <para>
/// Infrastructure dependencies (mapper, current user, tenant, clock, logger, unit of work) are
/// not piled into the constructor; they are resolved <b>lazily</b> through
/// <see cref="IServiceProvider"/>. A derived service only takes its own repository/manager
/// dependencies in its constructor.
/// </para>
/// <example>
/// <code>
/// public class CompanyAppService(IServiceProvider sp, ICompanyRepository companyRepository)
///     : EnsaAppService(sp), ICompanyAppService
/// {
///     public async Task&lt;CompanyDto&gt; GetAsync(int id, CancellationToken ct = default)
///     {
///         await CheckPermissionAsync(EnsaPermissions.Company.Default);
///         var company = await companyRepository.GetAsync(id, ct);
///         return ObjectMapper.Map&lt;Company, CompanyDto&gt;(company);
///     }
/// }
/// </code>
/// </example>
/// </summary>
public abstract class EnsaAppService : IApplicationService
{
    private readonly ConcurrentDictionary<Type, object> _resolvedServices = new();
    private ILogger? _logger;

    protected EnsaAppService(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ServiceProvider = serviceProvider;
    }

    /// <summary>The scoped service provider.</summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>Entity-to-DTO mapping (AutoMapper).</summary>
    protected IMapper ObjectMapper => LazyGetService<IMapper>();

    /// <summary>The currently signed-in user.</summary>
    protected ICurrentUser CurrentUser => LazyGetService<ICurrentUser>();

    /// <summary>The active organization (tenant) context.</summary>
    protected ICurrentTenant CurrentTenant => LazyGetService<ICurrentTenant>();

    /// <summary>Testable time source. Do NOT use <c>DateTime.Now</c>.</summary>
    protected IClock Clock => LazyGetService<IClock>();

    /// <summary>Transaction boundary. Call <c>SaveChangesAsync</c> after write operations.</summary>
    protected IUnitOfWork UnitOfWork => LazyGetService<IUnitOfWork>();

    /// <summary>Logger created with the service's own category name.</summary>
    protected ILogger Logger => _logger ??=
        LazyGetServiceOrNull<ILoggerFactory>()?.CreateLogger(GetType().FullName ?? GetType().Name)
        ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

    // ------------------------------------------------------------- Lazy DI

    /// <summary>
    /// Resolves the requested service lazily and caches it per instance.
    /// Throws <see cref="InvalidOperationException"/> when the service is not registered.
    /// </summary>
    protected T LazyGetService<T>() where T : notnull
        => (T)_resolvedServices.GetOrAdd(typeof(T), static (_, sp) => sp.GetRequiredService<T>(), ServiceProvider);

    /// <summary>Resolves the requested service lazily; returns <c>null</c> when it is not registered.</summary>
    protected T? LazyGetServiceOrNull<T>() where T : class
    {
        if (_resolvedServices.TryGetValue(typeof(T), out var cached))
        {
            return (T)cached;
        }

        var service = ServiceProvider.GetService<T>();
        if (service is not null)
        {
            _resolvedServices.TryAdd(typeof(T), service);
        }

        return service;
    }

    // --------------------------------------------------------- Authorization

    /// <summary>
    /// Throws <see cref="EnsaAuthorizationException"/> when the current user lacks the given permission.
    /// </summary>
    protected virtual Task CheckPermissionAsync(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        if (!CurrentUser.IsAuthenticated)
        {
            throw new EnsaAuthorizationException("You must sign in to perform this operation.", "Ensa:NotAuthenticated");
        }

        if (!CurrentUser.HasPermission(permission))
        {
            Logger.LogWarning(
                "Unauthorized access attempt. User={UserId}, Tenant={TenantId}, Permission={Permission}",
                CurrentUser.Id, CurrentUser.TenantId, permission);

            throw new EnsaAuthorizationException($"You do not have the '{permission}' permission.");
        }

        return Task.CompletedTask;
    }

    /// <summary>Throws when the user holds <b>none</b> of the given permissions.</summary>
    protected virtual Task CheckAnyPermissionAsync(params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        if (permissions.Length == 0)
        {
            return Task.CompletedTask;
        }

        if (!CurrentUser.IsAuthenticated)
        {
            throw new EnsaAuthorizationException("You must sign in to perform this operation.", "Ensa:NotAuthenticated");
        }

        if (!Array.Exists(permissions, CurrentUser.HasPermission))
        {
            throw new EnsaAuthorizationException(
                $"You must hold at least one of these permissions: {string.Join(", ", permissions)}");
        }

        return Task.CompletedTask;
    }

    /// <summary>Id of the current user; throws when there is no signed-in user.</summary>
    protected int GetRequiredUserId()
        => CurrentUser.Id ?? throw new EnsaAuthorizationException(
            "You must sign in to perform this operation.", "Ensa:NotAuthenticated");

    /// <summary>Id of the active tenant; throws when running in the host context.</summary>
    protected int GetRequiredTenantId()
        => CurrentTenant.Id ?? throw new TenantRequiredException();

    // ------------------------------------------------------------- Sorting

    /// <summary>
    /// Validates and normalizes the sort expression supplied by the client.
    /// <para>
    /// Accepted forms: <c>"Field"</c>, <c>"Field ASC"</c>, <c>"Field DESC"</c>, or several of them
    /// separated by commas: <c>"CompanyName ASC, CreationTime DESC"</c>.
    /// Invalid or empty expressions fall back to <paramref name="defaultSorting"/> — this keeps raw
    /// user input out of the ORDER BY clause and blocks SQL injection.
    /// </para>
    /// </summary>
    /// <param name="sorting">The raw expression received from the client.</param>
    /// <param name="defaultSorting">For example <c>"CreationTime DESC"</c>.</param>
    protected static string NormalizeSorting(string? sorting, string defaultSorting)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultSorting);

        if (string.IsNullOrWhiteSpace(sorting))
        {
            return defaultSorting;
        }

        var parts = sorting.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is 0 or > 4)
        {
            return defaultSorting;
        }

        var normalized = new List<string>(parts.Length);

        foreach (var part in parts)
        {
            var tokens = part.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length is 0 or > 2)
            {
                return defaultSorting;
            }

            if (!IsSafeMemberPath(tokens[0]))
            {
                return defaultSorting;
            }

            var direction = "ASC";
            if (tokens.Length == 2)
            {
                if (tokens[1].Equals("DESC", StringComparison.OrdinalIgnoreCase))
                {
                    direction = "DESC";
                }
                else if (!tokens[1].Equals("ASC", StringComparison.OrdinalIgnoreCase))
                {
                    return defaultSorting;
                }
            }

            normalized.Add($"{tokens[0]} {direction}");
        }

        return string.Join(", ", normalized);
    }

    /// <summary>Only paths made of letters, digits, <c>_</c> and <c>.</c> that start with a letter or <c>_</c> are accepted.</summary>
    private static bool IsSafeMemberPath(string value)
    {
        if (value.Length is 0 or > 128)
        {
            return false;
        }

        if (!char.IsLetter(value[0]) && value[0] != '_')
        {
            return false;
        }

        var previousWasDot = false;

        foreach (var c in value)
        {
            if (c == '.')
            {
                if (previousWasDot)
                {
                    return false;
                }

                previousWasDot = true;
                continue;
            }

            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                return false;
            }

            previousWasDot = false;
        }

        return !previousWasDot;
    }

    /// <summary>
    /// Rejects a calendar year that no <see cref="DateTime"/> can represent.
    /// <para>
    /// A missing <c>year</c> query parameter binds to <c>0</c>, and repositories build their range
    /// with <c>new DateTime(year, 1, 1)</c>, which throws <see cref="ArgumentOutOfRangeException"/>
    /// and surfaces as HTTP 500. Unvalidated input belongs in a 400, so every entry point that
    /// forwards a year to a repository calls this first.
    /// </para>
    /// </summary>
    /// <param name="year">Four-digit calendar year supplied by the caller.</param>
    protected static void ValidateCalendarYear(int year)
    {
        if (year < 1 || year > 9999)
        {
            throw new BusinessException(
                    "The year must be a four-digit calendar year.",
                    "Ensa:InvalidYear")
                .WithData("Year", year);
        }
    }

    /// <summary>Culture-invariant <c>int</c> to <c>string</c> conversion (used when building claims and ids).</summary>
    protected static string ToInvariant(int value) => value.ToString(CultureInfo.InvariantCulture);
}
