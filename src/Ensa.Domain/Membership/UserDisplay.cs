namespace Ensa.Domain.Membership;

/// <summary>
/// What a screen needs in order to show a user: a name, the account it belongs to, and whether
/// that account is still in use.
/// <para>
/// It exists because the pieces are no longer in one row. The name is on
/// <see cref="UserProfile"/>, the user name on <see cref="User"/>, and a list screen that shows
/// twenty people should not go looking for either twenty times.
/// </para>
/// </summary>
/// <param name="DisplayName">
/// Full name, falling back to the user name when the profile has none — or when there is no
/// profile, which is what a record from before the split looks like.
/// </param>
public readonly record struct UserDisplay(int Id, string DisplayName, string? UserName, bool IsActive);
