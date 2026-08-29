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

/// <summary>
/// The office (branch) the current call is executing for — the working context a user picks in the
/// shell, one level below the tenant.
/// <para>
/// <b>Everything on this interface is already validated.</b> The value arrives on the
/// <c>X-Ensa-OfficeId</c> request header, and the office resolution step rejects the request before
/// any application code runs when the office does not exist, is inactive, is soft-deleted, belongs
/// to another tenant, or is not one the caller may use. A service reading
/// <see cref="CurrentOfficeId"/> therefore never has to re-check it — which is the whole point of
/// having one place that does.
/// </para>
/// <para>
/// Switching office never switches tenant: the hierarchy is tenant → office → office-scoped data,
/// and <see cref="ICurrentTenant"/> stays untouched by office resolution.
/// </para>
/// </summary>
public interface ICurrentOffice
{
    /// <summary>
    /// Whether the request carried an office context at all.
    /// <para>
    /// <c>false</c> is not an error: most endpoints do not need one, and a request without the
    /// header runs unscoped inside its tenant, exactly as it did before the office context existed.
    /// </para>
    /// </summary>
    bool IsSpecified { get; }

    /// <summary>Whether one specific office was selected (as opposed to "all offices").</summary>
    bool HasOffice { get; }

    /// <summary>The selected office. <c>null</c> unless <see cref="HasOffice"/>.</summary>
    int? CurrentOfficeId { get; }

    /// <summary>
    /// Whether the caller explicitly asked for every office they are allowed to use
    /// (the UI's "Tüm Şubeler"). Only ever <c>true</c> when the server granted that scope.
    /// </summary>
    bool IsAllOffices { get; }

    /// <summary>
    /// The office ids a query must be restricted to. <b>Empty means no office predicate at all</b> —
    /// either because no office context was supplied, or because the caller's permitted scope is the
    /// whole tenant, in which case the tenant filter alone is already the right answer.
    /// </summary>
    IReadOnlyList<int> OfficeIds { get; }
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
