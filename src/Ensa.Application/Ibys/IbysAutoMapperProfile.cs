using AutoMapper;
using Ensa.Application.Contracts.Ibys.Dtos;
using Ensa.Domain.Ibys;

namespace Ensa.Application.Ibys;

/// <summary>
/// Mappings for the IBYS module.
/// <para>
/// <b>SECURITY.</b> There is deliberately no member mapping for <c>IbysQuery.XmlData</c> or
/// <c>IbysQuery.SignedData</c>: no DTO in this module declares those members, so the payloads
/// cannot leak through a mapping. Only their presence is projected, as
/// <c>HasXmlData</c> / <c>HasSignedData</c>. The e-signature licence key
/// (<c>ESignatureLicense.License</c>) has no mapping at all — it is a secret and never
/// crosses the application boundary.
/// </para>
/// <para>
/// There is also no input-to-entity mapping here. IBYS submissions are created by the
/// background submission worker in the domain, not by an API caller; the only state an
/// operator may change is the status, and that goes through
/// <c>IIbysSubmissionManager.ValidateStatusTransition</c>.
/// </para>
/// </summary>
public class IbysAutoMapperProfile : Profile
{
    public IbysAutoMapperProfile()
    {
        CreateMap<IbysQuery, IbysQueryDto>()
            .ForMember(d => d.HasXmlData, o => o.MapFrom(s => s.XmlData != null && s.XmlData != ""))
            .ForMember(d => d.HasSignedData, o => o.MapFrom(s => s.SignedData != null && s.SignedData != ""));

        CreateMap<IbysQuery, IbysQueryListDto>()
            // Display names are resolved with batched lookups in the application service.
            .ForMember(d => d.CompanyName, o => o.Ignore())
            .ForMember(d => d.EmployeeFullName, o => o.Ignore());
    }
}
