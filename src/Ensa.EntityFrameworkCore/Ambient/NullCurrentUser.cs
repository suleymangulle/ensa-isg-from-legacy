using Ensa.Domain.Common;

namespace Ensa.EntityFrameworkCore.Ambient;

/// <summary>
/// <see cref="ICurrentUser"/> null object representing an unidentified (anonymous) user.
/// <para>
/// Where it is used:
/// <list type="bullet">
/// <item>Design-time migration generation (<c>dotnet ef</c>)</item>
/// <item>DbMigrator / seed operations</item>
/// <item>Background jobs (job, worker) with no HTTP context</item>
/// </list>
/// </para>
/// <para>
/// <b>Note:</b> the real implementation, which works off <c>HttpContext.User</c> in the HTTP layer, is
/// registered inside <c>Ensa.HttpApi.Host</c> and overrides this null object. Because this service is added
/// to DI with <c>TryAdd</c>, the Host registration takes precedence.
/// </para>
/// </summary>
public sealed class NullCurrentUser : ICurrentUser
{
    /// <summary>Singleton instance.</summary>
    public static readonly NullCurrentUser Instance = new();

    /// <inheritdoc />
    public bool IsAuthenticated => false;

    /// <inheritdoc />
    public int? Id => null;

    /// <inheritdoc />
    public string? UserName => null;

    /// <inheritdoc />
    public string? Email => null;

    /// <inheritdoc />
    public int? TenantId => null;

    /// <inheritdoc />
    public int? CompanyId => null;

    /// <inheritdoc />
    public string[] Roles => [];

    /// <inheritdoc />
    public bool IsInRole(string roleName) => false;

    /// <inheritdoc />
    public bool HasPermission(string permissionName) => false;
}
