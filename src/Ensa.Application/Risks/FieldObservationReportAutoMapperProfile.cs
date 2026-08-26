using AutoMapper;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Domain.Risks;

namespace Ensa.Application.Risks;

/// <summary>
/// Field observation report and line mappings.
/// <para>
/// <c>SendMail</c> / <c>MailAddress</c> exist only on the input DTO (legacy <c>[NotMapped]</c>
/// fields) and have no entity counterpart, so they are simply absent from the destination.
/// </para>
/// </summary>
public class FieldObservationReportAutoMapperProfile : Profile
{
    public FieldObservationReportAutoMapperProfile()
    {
        // ---------------------------------------------------------- Report

        CreateMap<FieldObservationReport, FieldObservationReportDto>();

        CreateMap<FieldObservationReport, FieldObservationReportListDto>()
            // Resolved by batched lookups in the application service.
            .ForMember(d => d.CompanyName, o => o.Ignore())
            .ForMember(d => d.DepartmentName, o => o.Ignore())
            .ForMember(d => d.LineCount, o => o.Ignore());

        CreateMap<CreateFieldObservationReportDto, FieldObservationReport>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateFieldObservationReportDto, FieldObservationReport>()
            .IncludeBase<CreateFieldObservationReportDto, FieldObservationReport>();

        // ------------------------------------------------------------ Line

        CreateMap<FieldObservationLine, FieldObservationLineDto>()
            // Evaluated against the clock by the application service.
            .ForMember(d => d.IsOverdue, o => o.Ignore());

        CreateMap<CreateFieldObservationLineDto, FieldObservationLine>()
            .ForMember(d => d.FieldObservationReportId, o => o.Ignore())
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateFieldObservationLineDto, FieldObservationLine>()
            .IncludeBase<CreateFieldObservationLineDto, FieldObservationLine>();
    }
}
