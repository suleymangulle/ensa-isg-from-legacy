using AutoMapper;
using Ensa.Application.Contracts.Companies.Dtos;
using Ensa.Application.Contracts.Companies.Dtos.Navigations;
using Ensa.Domain.Companies;

namespace Ensa.Application.Companies;

/// <summary>
/// Mappings for the workplace department module.
/// <para>
/// Audit fields carry the same names on the base DTOs, so they map by convention.
/// On the input maps <c>Id</c>, <c>TenantId</c>, <c>Deletable</c> and every audit field are
/// ignored — <c>Deletable</c> is owned by the system (default departments are protected) and
/// must never be set from client input. Navigation DTOs are projected by hand in the
/// application service and are never mapped here.
/// </para>
/// </summary>
public class WorkplaceDepartmentAutoMapperProfile : Profile
{
    public WorkplaceDepartmentAutoMapperProfile()
    {
        CreateMap<WorkplaceDepartment, WorkplaceDepartmentDto>();

        CreateMap<WorkplaceDepartment, WorkplaceDepartmentListDto>()
            // The workplace name requires a join; the application service fills it in.
            .ForMember(d => d.CompanyName, o => o.Ignore());

        CreateMap<CreateWorkplaceDepartmentDto, WorkplaceDepartment>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.Deletable, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateWorkplaceDepartmentDto, WorkplaceDepartment>()
            .IncludeBase<CreateWorkplaceDepartmentDto, WorkplaceDepartment>();

        CreateMap<DepartmentDocument, DepartmentDocumentDto>();
    }
}
