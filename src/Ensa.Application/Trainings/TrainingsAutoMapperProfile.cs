using AutoMapper;
using Ensa.Application.Contracts.Trainings.Dtos;
using Ensa.Domain.Common;
using Ensa.Domain.Trainings;

namespace Ensa.Application.Trainings;

/// <summary>
/// Mappings for the trainings module (catalogue, topics, annual plans and remote-learning
/// progress).
/// <para>
/// Rules, as established by <c>CompanyAutoMapperProfile</c>: audit fields map by name from
/// the base DTOs; input DTO to entity mappings ignore <c>Id</c>, <c>TenantId</c> and every
/// audit field; navigation DTOs are projected by hand in the application service.
/// </para>
/// <para>
/// Hazard-class durations are never mapped as members. They are a normalised child
/// collection with its own table, so the application service loads and replaces them
/// explicitly — which is what keeps them a list on the DTO rather than three columns.
/// </para>
/// </summary>
public class TrainingsAutoMapperProfile : Profile
{
    public TrainingsAutoMapperProfile()
    {
        // -------------------------------------------------- Training catalogue

        CreateMap<Training, TrainingDto>()
            .ForMember(d => d.Durations, o => o.Ignore());

        CreateMap<Training, TrainingListDto>();

        CreateMap<CreateTrainingDto, Training>()
            .IgnoreSystemFields()
            .ForMember(d => d.IsActive, o => o.Ignore());

        CreateMap<UpdateTrainingDto, Training>()
            .IncludeBase<CreateTrainingDto, Training>()
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive));

        CreateMap<TrainingDuration, TrainingDurationDto>();
        CreateMap<TrainingTopicDuration, TrainingTopicDurationDto>();

        // ------------------------------------------------------------ Topics

        CreateMap<TrainingTopic, TrainingTopicDto>()
            .ForMember(d => d.Durations, o => o.Ignore());

        CreateMap<CreateTrainingTopicDto, TrainingTopic>()
            .IgnoreSystemFields()
            .ForMember(d => d.TrainingId, o => o.Ignore());

        CreateMap<UpdateTrainingTopicDto, TrainingTopic>()
            .IncludeBase<CreateTrainingTopicDto, TrainingTopic>();

        // ------------------------------------------------------ Training plan

        CreateMap<TrainingPlan, TrainingPlanDto>();

        CreateMap<TrainingPlan, TrainingPlanListDto>()
            // Resolved with batched lookups in the application service.
            .ForMember(d => d.CompanyName, o => o.Ignore())
            .ForMember(d => d.LineCount, o => o.Ignore());

        CreateMap<CreateTrainingPlanDto, TrainingPlan>()
            .IgnoreSystemFields()
            .ForMember(d => d.IsActive, o => o.Ignore())
            .ForMember(d => d.IsTransferred, o => o.Ignore());

        CreateMap<UpdateTrainingPlanDto, TrainingPlan>()
            .IncludeBase<CreateTrainingPlanDto, TrainingPlan>()
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive))
            .ForMember(d => d.IsTransferred, o => o.MapFrom(s => s.IsTransferred));

        CreateMap<TrainingPlanLine, TrainingPlanLineDto>();

        CreateMap<CreateTrainingPlanLineDto, TrainingPlanLine>()
            .IgnoreSystemFields()
            .ForMember(d => d.TrainingPlanId, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.Ignore())
            .ForMember(d => d.PreviousLineId, o => o.Ignore())
            // Approval workflow fields are written only by the approval methods.
            .ForMember(d => d.ApprovalStatus, o => o.Ignore())
            .ForMember(d => d.ForApprovalSenderUserId, o => o.Ignore())
            .ForMember(d => d.ApproverUserId, o => o.Ignore())
            .ForMember(d => d.ForApprovalSendingDate, o => o.Ignore())
            .ForMember(d => d.ApprovalDate, o => o.Ignore())
            // IBYS outcome is written only by the submission flow.
            .ForMember(d => d.IbysStatus, o => o.Ignore())
            .ForMember(d => d.IbysQueryId, o => o.Ignore())
            .ForMember(d => d.IbysStatusCode, o => o.Ignore())
            .ForMember(d => d.IbysMessage, o => o.Ignore());

        CreateMap<UpdateTrainingPlanLineDto, TrainingPlanLine>()
            .IncludeBase<CreateTrainingPlanLineDto, TrainingPlanLine>()
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive));

        // ------------------------------------------------ Employee progress

        CreateMap<EmployeeTrainingProgress, EmployeeTrainingProgressDto>();

        CreateMap<StartTrainingProgressDto, EmployeeTrainingProgress>()
            .IgnoreSystemFields()
            // Progress always starts from zero, whatever the caller sends.
            .ForMember(d => d.FirstTestCompleted, o => o.Ignore())
            .ForMember(d => d.FirstTestNote, o => o.Ignore())
            .ForMember(d => d.LatestTestCompleted, o => o.Ignore())
            .ForMember(d => d.LatestTestNote, o => o.Ignore())
            .ForMember(d => d.ElapsedDurationSeconds, o => o.Ignore())
            .ForMember(d => d.ActivePage, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.Ignore());
    }
}

/// <summary>
/// Shared ignore lists for the trainings module's input mappings.
/// </summary>
internal static class TrainingsMappingExtensions
{
    /// <summary>
    /// Ignores <c>Id</c>, <c>TenantId</c> and every audit field on a fully audited,
    /// tenant-scoped entity.
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
