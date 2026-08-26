using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Menus.Dtos;
using Ensa.Application.Contracts.Menus.Dtos.Navigations;

namespace Ensa.Application.Contracts.Menus;

/// <summary>Menu definitions and the menu rendered for the signed-in user.</summary>
public interface IMenuAppService : IApplicationService
{
    Task<MenuDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<MenuListDto>> GetListAsync(
        GetMenuListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the menu tree of the signed-in user for the given layout type.
    /// <para>
    /// The user id comes from the validated token, never from the request, so a caller cannot
    /// ask for someone else's menu. The repository filters the tree by staff role, organization
    /// type, subscription plan and enabled modules, then applies the personal overrides.
    /// </para>
    /// </summary>
    Task<MenuNavigationDto> GetUserMenuAsync(
        string menuTypeCode,
        CancellationToken cancellationToken = default);
}
