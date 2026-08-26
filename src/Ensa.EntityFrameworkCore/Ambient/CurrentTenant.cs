using Ensa.Domain.Common;

namespace Ensa.EntityFrameworkCore.Ambient;

/// <summary>Data carried by the ambient tenant context.</summary>
/// <param name="TenantId">Id of the current organization (tenant). <c>null</c> = host context.</param>
/// <param name="Name">Display name of the current organization (optional, for logging/UI).</param>
public sealed record TenantInfo(int? TenantId, string? Name);

/// <summary>
/// Low-level accessor holding the current tenant (ABP: ICurrentTenantAccessor).
/// <para>
/// <see cref="ICurrentTenant"/> is the read/scope API; this interface is the <b>writing</b> side. Middleware
/// that resolves the tenant from a token/claim uses this interface.
/// </para>
/// </summary>
public interface ICurrentTenantAccessor
{
    /// <summary>Current tenant details. <c>null</c> means we are in a host context.</summary>
    TenantInfo? Current { get; set; }
}

/// <summary>
/// <see cref="AsyncLocal{T}"/>-based tenant accessor.
/// Registered as a singleton; the state is kept isolated per request/async flow.
/// </summary>
public sealed class AsyncLocalCurrentTenantAccessor : ICurrentTenantAccessor
{
    private static readonly AsyncLocal<TenantInfo?> Holder = new();

    /// <summary>Process-wide shared instance (for design-time scenarios).</summary>
    public static readonly AsyncLocalCurrentTenantAccessor Instance = new();

    /// <inheritdoc />
    public TenantInfo? Current
    {
        get => Holder.Value;
        set => Holder.Value = value;
    }
}

/// <summary>
/// Default <see cref="ICurrentTenant"/> implementation.
/// <para>
/// It reads the value through <see cref="ICurrentTenantAccessor"/>. The scope opened by
/// <see cref="Change"/> returns to the previous tenant when <see cref="IDisposable.Dispose"/> is called; this
/// lets host administration screens reach another organization's data temporarily.
/// </para>
/// </summary>
public sealed class CurrentTenant(ICurrentTenantAccessor accessor) : ICurrentTenant
{
    private readonly ICurrentTenantAccessor _accessor = accessor;

    /// <inheritdoc />
    public int? Id => _accessor.Current?.TenantId;

    /// <inheritdoc />
    public string? Name => _accessor.Current?.Name;

    /// <inheritdoc />
    public bool IsAvailable => Id.HasValue;

    /// <inheritdoc />
    public IDisposable Change(int? tenantId, string? name = null)
    {
        var previous = _accessor.Current;
        _accessor.Current = new TenantInfo(tenantId, name);
        return new DisposeAction(() => _accessor.Current = previous);
    }
}

/// <summary>
/// Null object that always represents the host context.
/// Used for design time (migration generation) and for background jobs that have no tenant context.
/// </summary>
public sealed class NullCurrentTenant : ICurrentTenant
{
    /// <summary>Singleton instance.</summary>
    public static readonly NullCurrentTenant Instance = new();

    private NullCurrentTenant() { }

    /// <inheritdoc />
    public int? Id => null;

    /// <inheritdoc />
    public string? Name => null;

    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public IDisposable Change(int? tenantId, string? name = null) => DisposeAction.Empty;
}
