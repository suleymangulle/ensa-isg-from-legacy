using AutoMapper;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Domain.Risks;

namespace Ensa.Application.Risks;

/// <summary>
/// Risk assessment report mappings.
/// <para>
/// Rules: audit fields carry the same names on the base DTOs and map automatically; input DTOs
/// never write <c>Id</c>, <c>TenantId</c> or audit fields; computed members (risk level, validity,
/// resolved names) are ignored here and filled by the application service.
/// </para>
/// </summary>
public class RiskAssessmentReportAutoMapperProfile : Profile
{
    public RiskAssessmentReportAutoMapperProfile()
    {
        // ---------------------------------------------------------- Report

        CreateMap<RiskAssessmentReport, RiskAssessmentReportDto>()
            // Evaluated against the clock by the application service.
            .ForMember(d => d.IsValid, o => o.Ignore());

        CreateMap<RiskAssessmentReport, RiskAssessmentReportListDto>()
            // Resolved by a batched lookup / date arithmetic in the application service.
            .ForMember(d => d.CompanyName, o => o.Ignore())
            .ForMember(d => d.IsExpired, o => o.Ignore())
            .ForMember(d => d.RemainingDays, o => o.Ignore());

        CreateMap<CreateRiskAssessmentReportDto, RiskAssessmentReport>()
            // Computed by IRiskAssessmentManager.CalculateValidUntilDate.
            .ForMember(d => d.ValidityDate, o => o.Ignore())
            // Create always starts as a draft; updates carry the status explicitly.
            .ForMember(d => d.ApprovalStatus, o => o.Ignore())
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateRiskAssessmentReportDto, RiskAssessmentReport>()
            .IncludeBase<CreateRiskAssessmentReportDto, RiskAssessmentReport>()
            .ForMember(d => d.ApprovalStatus, o => o.MapFrom(s => s.ApprovalStatus));

        // ------------------------------------------------- Identified hazard

        CreateMap<IdentifiedHazard, IdentifiedHazardDto>()
            // Derived from the scores by IRiskAssessmentManager.DetermineLevel.
            .ForMember(d => d.RiskLevel, o => o.Ignore())
            .ForMember(d => d.ResidualRiskLevel, o => o.Ignore());

        CreateMap<CreateIdentifiedHazardDto, IdentifiedHazard>()
            .ForMember(d => d.RiskAssessmentReportId, o => o.Ignore())
            // Both scores are written by IRiskAssessmentManager.CalculateAsync.
            .ForMember(d => d.RiskScore, o => o.Ignore())
            .ForMember(d => d.ResidualRiskScore, o => o.Ignore())
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateIdentifiedHazardDto, IdentifiedHazard>()
            .IncludeBase<CreateIdentifiedHazardDto, IdentifiedHazard>();

        // -------------------------------------------------- Control measure

        CreateMap<ControlMeasure, ControlMeasureDto>();

        CreateMap<CreateControlMeasureDto, ControlMeasure>()
            .ForMember(d => d.IdentifiedHazardId, o => o.Ignore())
            // Completion goes through CompleteControlMeasureAsync, never through create.
            .ForMember(d => d.IsCompleted, o => o.Ignore())
            .ForMember(d => d.CompletionDate, o => o.Ignore())
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        // ------------------------------------------- Header child collections

        CreateMap<RiskAssessmentExposedGroup, RiskAssessmentExposedGroupDto>();
        CreateMap<RiskAssessmentControlMeasure, RiskAssessmentControlMeasureDto>();
        CreateMap<RiskAssessmentImprovementAction, RiskAssessmentImprovementActionDto>();
        CreateMap<RiskAssessmentProtectedGroup, RiskAssessmentProtectedGroupDto>();
        CreateMap<RiskAssessmentParticipant, RiskAssessmentParticipantDto>();
        CreateMap<RiskAssessmentHistoryRecord, RiskAssessmentHistoryRecordDto>();
    }
}
