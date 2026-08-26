using AutoMapper;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Domain.Risks;

namespace Ensa.Application.Risks;

/// <summary>
/// Emergency action plan, section and team member mappings.
/// <para>
/// Sections are not mapped from an input DTO: they are upserted one row per section type by
/// <c>SaveSectionAsync</c>, which builds the entity directly so it can set the print order.
/// </para>
/// </summary>
public class EmergencyActionPlanAutoMapperProfile : Profile
{
    public EmergencyActionPlanAutoMapperProfile()
    {
        // ------------------------------------------------------------ Plan

        CreateMap<EmergencyActionPlan, EmergencyActionPlanDto>()
            // Evaluated against the clock by the application service.
            .ForMember(d => d.IsValid, o => o.Ignore());

        CreateMap<EmergencyActionPlan, EmergencyActionPlanListDto>()
            // Resolved by a batched lookup / date arithmetic in the application service.
            .ForMember(d => d.ResolvedCompanyName, o => o.Ignore())
            .ForMember(d => d.IsExpired, o => o.Ignore())
            .ForMember(d => d.RemainingDays, o => o.Ignore());

        CreateMap<CreateEmergencyActionPlanDto, EmergencyActionPlan>()
            // Computed from the hazard class by IRiskAssessmentManager.CalculateValidUntilDate.
            .ForMember(d => d.ValidityDate, o => o.Ignore())
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateEmergencyActionPlanDto, EmergencyActionPlan>()
            .IncludeBase<CreateEmergencyActionPlanDto, EmergencyActionPlan>();

        // --------------------------------------------------------- Section

        CreateMap<EmergencyPlanSection, EmergencyPlanSectionDto>();

        // ----------------------------------------------------- Team member

        CreateMap<EmergencyTeamMember, EmergencyTeamMemberDto>();

        CreateMap<CreateEmergencyTeamMemberDto, EmergencyTeamMember>()
            .ForMember(d => d.EmergencyActionPlanId, o => o.Ignore())
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());
    }
}
