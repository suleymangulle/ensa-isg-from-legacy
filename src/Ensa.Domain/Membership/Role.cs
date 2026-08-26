using Ensa.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Ensa.Domain.Membership;

/// <summary>
/// User role. Derives from <see cref="IdentityRole{TKey}"/>; <c>Name</c>,
/// <c>NormalizedName</c> and <c>ConcurrencyStamp</c> come from the base class.
/// <para>
/// It has no direct legacy counterpart — permissions used to be granted through the
/// <c>UserType_T</c> + <c>UserPermission_T</c> pair. A role is a new grouping layer on top of
/// that model; the legacy permission tables remain in place.
/// </para>
/// <para><c>TenantId == null</c> → a host (system) role available to every tenant.</para>
/// </summary>
public class Role : IdentityRole<int>, IEntity<int>, IMultiTenant
{
    public string? Description { get; set; }

    /// <summary>A system-defined role that cannot be deleted or renamed.</summary>
    public bool IsStatic { get; set; }

    /// <summary>The default role assigned automatically to newly created users.</summary>
    public bool IsDefault { get; set; }

    /// <summary><c>null</c> = host role (available to every tenant).</summary>
    public int? TenantId { get; set; }

    public object?[] GetKeys() => [Id];

    public override string ToString() => $"[{nameof(Role)}] Id = {Id}";
}
