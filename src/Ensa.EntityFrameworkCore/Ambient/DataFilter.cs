using Ensa.Domain.Common;

namespace Ensa.EntityFrameworkCore.Ambient;

/// <summary>
/// Helper that satisfies the <see cref="IDisposable"/> contract with an <see cref="Action"/>.
/// Used to restore the previous value when managing ambient (AsyncLocal-based) scopes.
/// </summary>
public sealed class DisposeAction(Action action) : IDisposable
{
    private Action? _action = action;

    /// <summary>Singleton instance that does nothing.</summary>
    public static readonly DisposeAction Empty = new(static () => { });

    public void Dispose()
    {
        // Only the first Dispose call has an effect.
        var action = Interlocked.Exchange(ref _action, null);
        action?.Invoke();
    }
}

/// <summary>
/// AsyncLocal-based <see cref="IDataFilter"/> implementation.
/// <para>
/// Used to switch the global query filters (soft delete, multi-tenant) off temporarily at runtime. Because
/// the state is kept in an <see cref="AsyncLocal{T}"/>, it affects only the current async flow (and its
/// child flows).
/// </para>
/// <para>
/// Example:
/// <code>
/// using (_dataFilter.Disable&lt;ISoftDelete&gt;())
/// {
///     var deleted = await _repo.GetListAsync();
/// }
/// </code>
/// </para>
/// <para>
/// <b>Warning:</b> disabling a filter affects only <i>new</i> queries issued through
/// <see cref="EnsaDbContext"/>. It adds no extra cost, because EF Core's query compilation cache carries the
/// filter expression as a parameter.
/// </para>
/// </summary>
public sealed class DataFilter : IDataFilter
{
    /// <summary>
    /// Enabled/disabled state per filter type. A <c>null</c> value means every filter is enabled.
    /// The dictionary is updated by copying (copy-on-write) so that parallel async branches do not
    /// corrupt each other's state.
    /// </summary>
    private static readonly AsyncLocal<IReadOnlyDictionary<Type, bool>?> States = new();

    /// <summary>Process-wide shared instance (for design-time scenarios).</summary>
    public static readonly DataFilter Instance = new();

    /// <inheritdoc />
    public IDisposable Disable<TFilter>() where TFilter : class => SetEnabled(typeof(TFilter), false);

    /// <inheritdoc />
    public IDisposable Enable<TFilter>() where TFilter : class => SetEnabled(typeof(TFilter), true);

    /// <inheritdoc />
    public bool IsEnabled<TFilter>() where TFilter : class
    {
        var states = States.Value;
        if (states is not null && states.TryGetValue(typeof(TFilter), out var enabled))
        {
            return enabled;
        }

        // Default: every filter is enabled.
        return true;
    }

    private static IDisposable SetEnabled(Type filterType, bool enabled)
    {
        var previous = States.Value;

        if (previous is not null && previous.TryGetValue(filterType, out var current) && current == enabled)
        {
            // Already in the requested state; there is nothing to restore at the end of the scope.
            return DisposeAction.Empty;
        }

        var next = previous is null
            ? new Dictionary<Type, bool>()
            : new Dictionary<Type, bool>(previous);

        next[filterType] = enabled;
        States.Value = next;

        return new DisposeAction(() => States.Value = previous);
    }
}
