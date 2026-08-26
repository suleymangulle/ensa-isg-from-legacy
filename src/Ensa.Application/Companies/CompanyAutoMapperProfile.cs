using AutoMapper;
using Ensa.Application.Contracts.Companies.Dtos;
using Ensa.Domain.Companies;

namespace Ensa.Application.Companies;

/// <summary>
/// Mappings for the company module — the <b>reference profile</b> for the other modules.
/// <para>
/// Rules:
/// <list type="bullet">
/// <item>Audit fields (<c>CreationTime</c>, <c>CreatorId</c> and so on) map automatically because
/// the base DTOs declare them under the same names.</item>
/// <item>When mapping an input DTO onto an entity, <c>Id</c>, <c>TenantId</c> and the audit fields
/// are <b>ignored</b> — the interceptor and the manager own those values.</item>
/// <item>Navigation DTOs are not mapped here; the application service projects them by hand.</item>
/// </list>
/// </para>
/// </summary>
public class CompanyAutoMapperProfile : Profile
{
    public CompanyAutoMapperProfile()
    {
        CreateMap<Company, CompanyDto>();

        CreateMap<Company, CompanyListDto>()
            // City and district names need a join; the repository fills them in for the list query.
            .ForMember(d => d.CityName, o => o.Ignore())
            .ForMember(d => d.DistrictName, o => o.Ignore())
            .ForMember(d => d.WorkerCount, o => o.Ignore());

        CreateMap<CreateCompanyDto, Company>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateCompanyDto, Company>()
            .IncludeBase<CreateCompanyDto, Company>();
    }
}
