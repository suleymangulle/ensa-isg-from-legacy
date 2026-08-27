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
public sealed record UserListQuery(
    string? Search,
    StaffRole? StaffRole,
    int? OfficeId,
    int? CompanyId,
    bool? IsActive,
    int SkipCount,
    int MaxResultCount);

/// <summary>One row of that list: the account and the two records that describe the person.</summary>
public sealed record UserListRow(User Account, UserProfile? Profile, UserEmployment? Employment);
