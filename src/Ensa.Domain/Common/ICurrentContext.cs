namespace Ensa.Domain.Common;

/// <summary>The tenant the current call is executing for (ABP: <c>ICurrentTenant</c>).</summary>
public interface ICurrentTenant
{
    int? Id { get; }
    string? Name { get; }
    bool IsAvailable { get; }

    /// <summary>
    /// Runs the enclosing scope against another tenant; passing <c>null</c> switches to the host
    /// context. Restores the previous tenant on dispose.
    /// </summary>
    IDisposable Change(int? tenantId, string? name = null);
}

/// <summary>The signed-in user of the current call (ABP: <c>ICurrentUser</c>).</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int? Id { get; }
    string? UserName { get; }
    string? Email { get; }
    int? TenantId { get; }

    /// <summary>
    /// The client workplace the user belongs to, or <c>null</c> for the provider's own staff.
    /// A value here narrows every company-scoped query to that one workplace.
    /// </summary>
    int? CompanyId { get; }

    string[] Roles { get; }

    bool IsInRole(string roleName);
    bool HasPermission(string permissionName);
}

/// <summary>Injectable clock, so time-dependent rules stay testable (ABP: <c>IClock</c>).</summary>
public interface IClock
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}

/// <summary>
/// Temporarily suspends a global query filter such as soft delete or tenant isolation
/// (ABP: <c>IDataFilter</c>).
/// <para>
/// Disabling <see cref="IMultiTenant"/> removes tenant isolation for the whole scope, so each use
/// must be deliberate, narrow, and justified in the calling method's XML doc.
/// </para>
/// </summary>
public interface IDataFilter
{
    IDisposable Disable<TFilter>() where TFilter : class;
    IDisposable Enable<TFilter>() where TFilter : class;
    bool IsEnabled<TFilter>() where TFilter : class;
}
