using AutoMapper;
using Ensa.Application.Contracts.Tenancy.Dtos;
using Ensa.Domain.Tenancy;

namespace Ensa.Application.Tenancy;

/// <summary>
/// Mappings for the office module.
/// <para>
/// Audit fields carry the same names on the base DTOs, so they map by convention.
/// On the input maps <c>Id</c>, <c>TenantId</c> and every audit field are ignored — the
/// interceptor owns those. Navigation DTOs are projected by hand in the application service.
/// </para>
/// </summary>
public class OfficeAutoMapperProfile : Profile
{
    public OfficeAutoMapperProfile()
    {
        CreateMap<Office, OfficeDto>();

        CreateMap<Office, OfficeListDto>()
            // City and district names require a join; the detail projection resolves them.
            .ForMember(d => d.CityName, o => o.Ignore())
            .ForMember(d => d.DistrictName, o => o.Ignore());

        CreateMap<CreateOfficeDto, Office>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateOfficeDto, Office>()
            .IncludeBase<CreateOfficeDto, Office>();
    }
}
