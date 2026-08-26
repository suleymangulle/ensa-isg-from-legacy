using AutoMapper;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Domain.Risks;

namespace Ensa.Application.Risks;

/// <summary>
/// Incident and incident-person mappings.
/// <para>
/// The SSI notification figures are ignored here on purpose: they are calculated by
/// <c>IIncidentManager</c> and filled by the application service, never mapped from a column.
/// </para>
/// </summary>
public class IncidentAutoMapperProfile : Profile
{
    public IncidentAutoMapperProfile()
    {
        // -------------------------------------------------------- Incident

        CreateMap<Incident, IncidentDto>()
            .ForMember(d => d.LatestSsiNotificationDate, o => o.Ignore())
            .ForMember(d => d.SsiNotificationOverdue, o => o.Ignore())
            .ForMember(d => d.RemainingSsiNotificationWorkDays, o => o.Ignore());

        CreateMap<Incident, IncidentListDto>()
            .ForMember(d => d.CompanyName, o => o.Ignore())
            .ForMember(d => d.DepartmentName, o => o.Ignore())
            .ForMember(d => d.LatestSsiNotificationDate, o => o.Ignore())
            .ForMember(d => d.SsiNotificationOverdue, o => o.Ignore());

        CreateMap<CreateIncidentDto, Incident>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore());

        CreateMap<UpdateIncidentDto, Incident>()
            .IncludeBase<CreateIncidentDto, Incident>();

        // -------------------------------------------------- Incident person

        CreateMap<IncidentPerson, IncidentPersonDto>();

        CreateMap<CreateIncidentPersonDto, IncidentPerson>()
            .ForMember(d => d.IncidentId, o => o.Ignore())
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
