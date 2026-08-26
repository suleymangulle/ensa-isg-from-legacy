using AutoMapper;
using Ensa.Application.Contracts.Companies.Dtos;
using Ensa.Application.Contracts.Companies.Dtos.Navigations;
using Ensa.Domain.Companies;
using Ensa.Domain.Companies.Navigations;

namespace Ensa.Application.Companies;

/// <summary>
/// Mappings for the company employee module.
/// <para>
/// Audit fields carry the same names on the base DTOs, so they map by convention.
/// On the input maps <c>Id</c>, <c>TenantId</c> and every audit field are ignored — the
/// interceptor and the Manager own those. Navigation DTOs are projected by hand in the
/// application service and are never mapped here.
/// </para>
/// </summary>
public class CompanyEmployeeAutoMapperProfile : Profile
{
    public CompanyEmployeeAutoMapperProfile()
    {
        CreateMap<CompanyEmployee, CompanyEmployeeDto>();

        CreateMap<CompanyEmployee, CompanyEmployeeListDto>()
            // The workplace name requires a join; the application service fills it in.
            .ForMember(d => d.CompanyName, o => o.Ignore());

        CreateMap<CreateCompanyEmployeeDto, CompanyEmployee>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateCompanyEmployeeDto, CompanyEmployee>()
            .IncludeBase<CreateCompanyEmployeeDto, CompanyEmployee>();

        // ---- Normalized health sub-records (read-only projections) ----

        CreateMap<EmployeeHealthInfo, EmployeeHealthInfoDto>();
        CreateMap<EmployeeImmunization, EmployeeImmunizationDto>();
        CreateMap<EmployeeFamilyHistory, EmployeeFamilyHistoryDto>();
        CreateMap<EmployeeWorkHistory, EmployeeWorkHistoryDto>();
        CreateMap<CompanyEmployeeDuty, CompanyEmployeeDutyDto>();

        // Query projection, not an entity — see EmployeeLatestTrainingInfo.
        CreateMap<EmployeeLatestTrainingInfo, EmployeeLatestTrainingInfoDto>();
    }
}
