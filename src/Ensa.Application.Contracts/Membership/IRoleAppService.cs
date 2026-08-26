using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Membership.Dtos;

namespace Ensa.Application.Contracts.Membership;

/// <summary>
/// Role management. Roles are an ASP.NET Core Identity concept, so every write goes
/// through <c>RoleManager&lt;Role&gt;</c> — never through a repository — so that the
/// normalized name and concurrency stamp stay consistent.
/// </summary>
public interface IRoleAppService : IApplicationService
{
    Task<RoleDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<RoleListDto>> GetListAsync(
        GetRoleListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Lightweight records for drop-downs and role pickers.</summary>
    Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default);

    Task<RoleDto> CreateAsync(CreateRoleDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the role. Static roles (<c>IsStatic == true</c>) cannot be renamed —
    /// the attempt fails with <c>Ensa:Role:SystemRoleImmutable</c>.
    /// </summary>
    Task<RoleDto> UpdateAsync(int id, UpdateRoleDto input, CancellationToken cancellationToken = default);

    /// <summary>Deletes the role. Static roles cannot be deleted.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
