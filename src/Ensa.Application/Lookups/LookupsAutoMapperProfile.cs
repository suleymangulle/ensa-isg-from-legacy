using AutoMapper;
using Ensa.Application.Contracts.Lookups.Dtos;
using Ensa.Domain.Lookups;

namespace Ensa.Application.Lookups;

/// <summary>
/// Mappings for the reference-data module.
/// <para>
/// The lookup endpoints project straight into <c>LookupDto</c> shapes by hand, because each
/// reference table names its display column differently (<c>CityName</c>, <c>DutyName</c>,
/// <c>CertificateName</c> ...) and a per-table map would add no clarity. Only
/// <see cref="Parameter"/>, the one writable entity of the module, is mapped here.
/// </para>
/// </summary>
public class LookupsAutoMapperProfile : Profile
{
    public LookupsAutoMapperProfile()
    {
        CreateMap<Parameter, ParameterDto>();

        CreateMap<Parameter, ParameterListDto>();

        CreateMap<ParameterInputDto, Parameter>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            // The code identifies the parameter to application code, so it is set only on
            // create and never rewritten by an update payload.
            .ForMember(d => d.Code, o => o.Ignore());

        CreateMap<CreateParameterDto, Parameter>()
            .IncludeBase<ParameterInputDto, Parameter>()
            .ForMember(d => d.Code, o => o.MapFrom(s => s.Code));

        CreateMap<UpdateParameterDto, Parameter>()
            .IncludeBase<ParameterInputDto, Parameter>();
    }
}
