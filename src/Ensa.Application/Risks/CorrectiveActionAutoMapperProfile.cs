using AutoMapper;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Domain.Risks;

namespace Ensa.Application.Risks;

/// <summary>
/// Corrective / preventive action (DOF) mappings.
/// <para>
/// The closing fields (<c>Result</c>, <c>ResultDate</c>, <c>OperationResult</c>) are deliberately
/// ignored on the input maps: closing is a state transition owned by <c>CloseAsync</c>, not a
/// property an edit form may overwrite.
/// </para>
/// </summary>
public class CorrectiveActionAutoMapperProfile : Profile
{
    public CorrectiveActionAutoMapperProfile()
    {
        CreateMap<CorrectiveAction, CorrectiveActionDto>()
            // Evaluated against the clock by the application service.
            .ForMember(d => d.IsOverdue, o => o.Ignore());

        CreateMap<CorrectiveAction, CorrectiveActionListDto>()
            .ForMember(d => d.CompanyName, o => o.Ignore())
            .ForMember(d => d.IsOverdue, o => o.Ignore());

        CreateMap<CreateCorrectiveActionDto, CorrectiveAction>()
            .ForMember(d => d.Result, o => o.Ignore())
            .ForMember(d => d.ResultDate, o => o.Ignore())
            .ForMember(d => d.ResultDocumentId, o => o.Ignore())
            .ForMember(d => d.OperationResult, o => o.Ignore())
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateCorrectiveActionDto, CorrectiveAction>()
            .IncludeBase<CreateCorrectiveActionDto, CorrectiveAction>()
            // The result document may be attached while the action is still open.
            .ForMember(d => d.ResultDocumentId, o => o.MapFrom(s => s.ResultDocumentId));
    }
}
