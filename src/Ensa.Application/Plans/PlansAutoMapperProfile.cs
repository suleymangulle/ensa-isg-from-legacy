using AutoMapper;
using Ensa.Application.Contracts.Plans.Dtos;
using Ensa.Domain.Common;
using Ensa.Domain.Plans;

namespace Ensa.Application.Plans;

/// <summary>
/// Mappings for the plans module (annual work plans and the activity catalogue).
/// <para>
/// Rules, as established by <c>CompanyAutoMapperProfile</c>: audit fields map by name from
/// the base DTOs; input DTO to entity mappings ignore <c>Id</c>, <c>TenantId</c> and every
/// audit field; navigation DTOs are projected by hand in the application service.
/// </para>
/// <para>
/// <c>TenantId</c> is ignored on every input mapping, which matters here because
/// <c>Activity</c> is a mixed host/tenant catalogue: letting a caller set the tenant would
/// let them write into the shared host catalogue or into another organisation's entries.
/// The save interceptor assigns it from the ambient tenant instead.
/// </para>
/// </summary>
public class PlansAutoMapperProfile : Profile
{
    public PlansAutoMapperProfile()
    {
        // ---------------------------------------------------------- Work plan

        CreateMap<WorkPlan, WorkPlanDto>();

        CreateMap<WorkPlan, WorkPlanListDto>()
            // Resolved with batched lookups in the application service.
            .ForMember(d => d.CompanyName, o => o.Ignore())
            .ForMember(d => d.LineCount, o => o.Ignore());

        CreateMap<CreateWorkPlanDto, WorkPlan>()
            .IgnoreSystemFields()
            .ForMember(d => d.IsActive, o => o.Ignore())
            .ForMember(d => d.IsTransferred, o => o.Ignore());

        CreateMap<UpdateWorkPlanDto, WorkPlan>()
            .IncludeBase<CreateWorkPlanDto, WorkPlan>()
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive))
            .ForMember(d => d.IsTransferred, o => o.MapFrom(s => s.IsTransferred));

        CreateMap<WorkPlanLine, WorkPlanLineDto>();

        CreateMap<CreateWorkPlanLineDto, WorkPlanLine>()
            .IgnoreSystemFields()
            .ForMember(d => d.WorkPlanId, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.Ignore())
            .ForMember(d => d.PreviousLineId, o => o.Ignore())
            // Approval workflow fields are written only by IWorkPlanManager.
            .ForMember(d => d.ApprovalStatus, o => o.Ignore())
            .ForMember(d => d.ForApprovalSenderUserId, o => o.Ignore())
            .ForMember(d => d.ApproverUserId, o => o.Ignore())
            .ForMember(d => d.ForApprovalSendingDate, o => o.Ignore())
            .ForMember(d => d.ApprovalDate, o => o.Ignore());

        CreateMap<UpdateWorkPlanLineDto, WorkPlanLine>()
            .IncludeBase<CreateWorkPlanLineDto, WorkPlanLine>()
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive));

        // --------------------------------------------------- Activity catalogue

        CreateMap<Activity, ActivityDto>();

        CreateMap<Activity, ActivityListDto>();

        CreateMap<CreateActivityDto, Activity>()
            .IgnoreSystemFields()
            .ForMember(d => d.IsActive, o => o.Ignore());

        CreateMap<UpdateActivityDto, Activity>()
            .IncludeBase<CreateActivityDto, Activity>()
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive));
    }
}

/// <summary>
/// Shared ignore list for the plans module's input mappings.
/// </summary>
internal static class PlansMappingExtensions
{
    /// <summary>
    /// Ignores <c>Id</c>, <c>TenantId</c> and every audit field on a fully audited,
    /// tenant-scoped entity. These are set by the save interceptor.
    /// </summary>
    public static IMappingExpression<TSource, TDestination> IgnoreSystemFields<TSource, TDestination>(
        this IMappingExpression<TSource, TDestination> map)
        where TDestination : FullAuditedTenantEntity
        => map
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
