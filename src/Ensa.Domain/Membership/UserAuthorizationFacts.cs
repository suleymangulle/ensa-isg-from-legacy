namespace Ensa.Domain.Membership;

/// <summary>
/// The five things authorization needs to know about a user, in one answer.
/// <para>
/// <b>Why this type exists.</b> These facts used to be five columns on <c>User</c>, so any code
/// that had a user had them for free. They now live where they belong — the account in
/// <c>User</c>, the person in <c>UserProfile</c>, the contract in <c>UserEmployment</c>, and being
/// an administrator in <c>UserRole</c>, because Identity owns roles. Asking four tables at every
/// call site would be four chances to ask the wrong one, so the question is asked once, here.
/// </para>
/// <para>
/// <b>Why <c>UserTypeId</c> and not a staff role.</b> The manager used to take the user's
/// <c>StaffRole</c> enum and search <c>UserType</c> for a row carrying the same enum — an indirect
/// lookup for a link that now exists directly. The same fact was stored in two places and free to
/// disagree; this reads the link.
/// </para>
/// </summary>
/// <param name="IsActive">
/// Whether the account may be used at all. A deactivated user holds no permissions, whatever the
/// gates would otherwise say.
/// </param>
/// <param name="IsDeleted">Soft-deleted accounts are treated exactly like deactivated ones.</param>
/// <param name="IsSystemAdministrator">
/// The legacy <c>SerAdmin</c>: it skips every gate. Read from the role assignment rather than a
/// boolean, so there is one answer to "is this person an administrator" rather than two.
/// </param>
/// <param name="UserTypeId">
/// Which kind of user this is. The user-type gate and the restriction rules both key off it;
/// <c>null</c> means the user has no type, and only unrestricted permissions apply.
/// </param>
/// <param name="TenantId">The tenant the user belongs to; <c>null</c> for a host user.</param>
public readonly record struct UserAuthorizationFacts(
    bool IsActive,
    bool IsDeleted,
    bool IsSystemAdministrator,
    int? UserTypeId,
    int? TenantId)
{
    /// <summary>Whether the account is usable at all — the first gate, before any permission.</summary>
    public bool CanAct => IsActive && !IsDeleted;
}
