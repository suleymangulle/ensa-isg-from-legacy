using AutoMapper;
using Ensa.Application.Contracts.Tenancy.Dtos;
using Ensa.Application.Contracts.Tenancy.Dtos.Navigations;
using Ensa.Domain.Tenancy;

namespace Ensa.Application.Tenancy;

/// <summary>
/// Mappings for the organization (tenant) module.
/// <para>
/// Audit fields carry the same names on the base DTOs, so they map by convention.
/// <see cref="Organization"/> is a host entity, so there is no <c>TenantId</c> to ignore.
/// On the input maps <c>Id</c> and every audit field are ignored — the interceptor owns those.
/// Navigation DTOs are projected by hand in the application service.
/// </para>
/// </summary>
public class OrganizationAutoMapperProfile : Profile
{
    public OrganizationAutoMapperProfile()
    {
        CreateMap<Organization, OrganizationDto>();

        CreateMap<Organization, OrganizationListDto>()
            // Type and plan names require a join; the detail projection resolves them.
            .ForMember(d => d.OrganizationTypeName, o => o.Ignore())
            .ForMember(d => d.SubscriptionPlanName, o => o.Ignore());

        CreateMap<CreateOrganizationDto, Organization>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateOrganizationDto, Organization>()
            .IncludeBase<CreateOrganizationDto, Organization>();

        // Read-only projection used by the detail screen.
        CreateMap<OrganizationContract, OrganizationContractSummaryDto>();
    }
}
