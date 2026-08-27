using Ensa.Domain.Common;

namespace Ensa.Domain.Membership;

/// <summary>
/// What Ensa knows about a role that ASP.NET Core Identity does not.
/// <para>
/// <b>Why it is not on the role.</b> Identity's <c>IdentityRole</c> carries a name, a normalized
/// name and a concurrency stamp, and nothing else belongs there — the same rule that emptied the
/// user table. A description and two behaviour flags are ours, so they live in our own table.
/// </para>
/// <para>
/// <b>What the flags do.</b> <see cref="IsStatic"/> protects a role the application itself relies
/// on: it may not be renamed or deleted, because code elsewhere asks for it by name.
/// <see cref="IsDefault"/> marks a role new users receive without anybody choosing it.
/// </para>
/// </summary>
public class RoleProfile : AuditedEntity
{
    /// <summary>The role this belongs to. FK — no navigation property.</summary>
    public int RoleId { get; set; }

    /// <summary>Shown on the role administration screen; searched on.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// The application depends on this role by name, so it may not be renamed or deleted.
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>Given to a new user unless something else is chosen.</summary>
    public bool IsDefault { get; set; }

    public override string ToString() => $"[{nameof(RoleProfile)}] RoleId = {RoleId}";
}
