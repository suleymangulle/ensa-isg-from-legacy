using AutoMapper;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Domain.Risks;

namespace Ensa.Application.Risks;

/// <summary>
/// Work equipment and equipment document mappings.
/// <para>
/// <c>NextExaminationDate</c> is never taken from the input: it is derived from the last
/// inspection date and the selected period by the application service.
/// </para>
/// </summary>
public class EquipmentAutoMapperProfile : Profile
{
    public EquipmentAutoMapperProfile()
    {
        // ------------------------------------------------------- Equipment

        CreateMap<Equipment, EquipmentDto>()
            // Evaluated against the clock by the application service.
            .ForMember(d => d.IsInspectionOverdue, o => o.Ignore());

        CreateMap<Equipment, EquipmentListDto>()
            .ForMember(d => d.CompanyName, o => o.Ignore())
            .ForMember(d => d.IsInspectionOverdue, o => o.Ignore())
            .ForMember(d => d.RemainingDays, o => o.Ignore());

        CreateMap<CreateEquipmentDto, Equipment>()
            // Derived from ExaminationDate + Period.
            .ForMember(d => d.NextExaminationDate, o => o.Ignore())
            // Only integrations may lock a record against deletion; create keeps the default.
            .ForMember(d => d.IsDeletable, o => o.Ignore())
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateEquipmentDto, Equipment>()
            .IncludeBase<CreateEquipmentDto, Equipment>()
            .ForMember(d => d.IsDeletable, o => o.MapFrom(s => s.IsDeletable));

        // ---------------------------------------------- Equipment document

        CreateMap<EquipmentDocument, EquipmentDocumentDto>();

        CreateMap<CreateEquipmentDocumentDto, EquipmentDocument>()
            .ForMember(d => d.EquipmentId, o => o.Ignore())
            // Denormalized from the parent equipment by the application service.
            .ForMember(d => d.CompanyId, o => o.Ignore())
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
