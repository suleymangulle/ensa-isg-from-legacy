using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Menus;
using Ensa.Application.Contracts.Menus.Dtos;
using Ensa.Application.Contracts.Menus.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>Menu endpoints - <c>api/menu</c>.</summary>
public class MenuController(IMenuAppService menuAppService) : EnsaController
{
    /// <summary>Returns a single menu definition.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<MenuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MenuDto> GetAsync(int id, CancellationToken cancellationToken)
        => menuAppService.GetAsync(id, cancellationToken);

    /// <summary>Paged, filterable menu list (menu administration).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<MenuListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<MenuListDto>> GetListAsync(
        [FromQuery] GetMenuListInput input,
        CancellationToken cancellationToken)
        => menuAppService.GetListAsync(input, cancellationToken);

    /// <summary>
    /// The menu of the signed-in user for the requested layout type, ready to render.
    /// <para>
    /// Carries <b>no permission policy</b> - only the inherited <c>[Authorize]</c>, which
    /// requires a valid token. This is the navigation shell every user needs right after
    /// signing in, and <c>Ensa.Menu</c> is the menu <i>administration</i> permission, so
    /// requiring it would leave ordinary users unable to navigate at all. The returned tree is
    /// already filtered for the caller inside the repository (staff role, organization type,
    /// subscription plan, enabled company modules and personal overrides), and the user id is
    /// taken from the token rather than from the request, so no caller can see another user's
    /// menu or an entry it is not entitled to.
    /// </para>
    /// </summary>
    [HttpGet("my-menu")]
    [ProducesResponseType<MenuNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<MenuNavigationDto> GetUserMenuAsync(
        [FromQuery] GetUserMenuInput input,
        CancellationToken cancellationToken)
        => menuAppService.GetUserMenuAsync(input.MenuTypeCode, cancellationToken);
}
