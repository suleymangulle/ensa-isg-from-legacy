using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Membership.Dtos;
using Ensa.Application.Contracts.Membership.Dtos.Navigations;

namespace Ensa.Application.Contracts.Membership;

/// <summary>
/// Administrative user management. Self-service operations (own profile, own password)
/// belong to <see cref="IAccountAppService"/>.
/// </summary>
public interface IUserAppService : IApplicationService
{
    Task<UserDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Combined detail view: user, organization, offices, roles, effective permissions.</summary>
    Task<UserNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<UserListDto>> GetListAsync(
        GetUserListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Lightweight records for drop-downs.</summary>
    Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(CreateUserDto input, CancellationToken cancellationToken = default);

    Task<UserDto> UpdateAsync(int id, UpdateUserDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Administrative password reset. The current password is not required, so this is
    /// guarded by <c>Ensa.User.Update</c>; the security stamp is rotated afterwards, which
    /// invalidates every outstanding refresh token of that user.
    /// </summary>
    Task ResetPasswordAsync(int id, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>Replaces the complete role set of the user with <paramref name="roles"/>.</summary>
    Task AssignRolesAsync(int id, string[] roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates or deactivates the user. A deactivated user keeps its records but can no
    /// longer obtain a token (see <c>UserExtensions.CanSignIn</c>).
    /// </summary>
    Task SetActiveStateAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}
