using AutoMapper;
using Ensa.Application.Contracts.Menus.Dtos;
using Ensa.Domain.Menus;

namespace Ensa.Application.Menus;

/// <summary>
/// Mappings for the menu module.
/// <para>
/// Read-only by design: menus, placements and catalogue entries are host reference data
/// seeded together with the permission catalogue, so this module exposes no create or update
/// input DTO and therefore needs no input-to-entity map.
/// </para>
/// <para>
/// The menu tree is not mapped here either - a recursive structure has to be walked, so it is
/// projected by hand inside <see cref="MenuAppService"/>.
/// </para>
/// </summary>
public class MenuAutoMapperProfile : Profile
{
    public MenuAutoMapperProfile()
    {
        CreateMap<Menu, MenuDto>();

        CreateMap<Menu, MenuListDto>();

        CreateMap<MenuItem, MenuItemDto>();
    }
}
