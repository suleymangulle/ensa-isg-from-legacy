using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Membership;

/// <summary>
/// What a user list screen asks for.
/// <para>
/// It is a request object rather than a predicate because the fields it filters on now live in
/// four different tables -- the account, the profile, the employment and the office assignments --
/// and an <c>Expression&lt;Func&lt;User, bool&gt;&gt;</c> handed in from the application layer can
/// only speak about one of them.
/// </para>
/// </summary>
/// <param name="OfficeIds">
/// The offices the list is restricted to. <b>Empty or <c>null</c> means no office restriction.</b>
/// It is a set rather than a single id because the office context can legitimately span several --
/// a user assigned to two offices who chose "all offices" is scoped to exactly those two, not to
/// the whole tenant.
/// </param>
public sealed record UserListQuery(
    string? Search,
    StaffRole? StaffRole,
    IReadOnlyList<int>? OfficeIds,
    int? CompanyId,
    bool? IsActive,
    int SkipCount,
    int MaxResultCount);

/// <summary>One row of that list: the account and the two records that describe the person.</summary>
public sealed record UserListRow(User Account, UserProfile? Profile, UserEmployment? Employment);
