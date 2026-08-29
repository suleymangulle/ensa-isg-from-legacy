using AutoMapper;
using Ensa.Application.Contracts.Communication.Dtos;
using Ensa.Domain.Communication;

namespace Ensa.Application.Communication;

/// <summary>
/// Mappings for the Communication module (mail queue, in-app messages, visits, support tickets).
/// <para>
/// Rules, following <c>CompanyAutoMapperProfile</c>:
/// <list type="bullet">
/// <item>Audit fields carry the same names on the base DTOs, so they map automatically on reads.</item>
/// <item>Input DTO to entity mappings <b>ignore</b> <c>Id</c>, <c>TenantId</c> and every audit
/// field.</item>
/// <item>Identity-bearing fields (message sender, ticket opener) are ignored on the way in and
/// set from the session by the service — see the remarks on those services.</item>
/// <item>Navigation DTOs are not mapped here; the app service projects them by hand.</item>
/// </list>
/// </para>
/// </summary>
public class CommunicationAutoMapperProfile : Profile
{
    public CommunicationAutoMapperProfile()
    {
        ConfigureMail();
        ConfigureMessage();
        ConfigureVisit();
        ConfigureSupportTicket();
    }

    private void ConfigureMail()
    {
        CreateMap<Mail, MailDto>();
        CreateMap<Mail, MailListDto>();
        CreateMap<MailAttachment, MailAttachmentDto>();

        CreateMap<CreateMailDto, Mail>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore())
            // Lifecycle fields belong to Queue/MarkSent/MarkFailed, never to a plain write.
            .ForMember(d => d.MailStatus, o => o.Ignore())
            .ForMember(d => d.ErrorMessage, o => o.Ignore())
            .ForMember(d => d.SubmissionDate, o => o.Ignore())
            .ForMember(d => d.AttemptCount, o => o.Ignore());

        CreateMap<UpdateMailDto, Mail>()
            .IncludeBase<CreateMailDto, Mail>();

        // AddMailAttachmentDto has no entity mapping: the attachment row is built explicitly so
        // the owning mail id comes from the route and the order number can be defaulted.
    }

    private void ConfigureMessage()
    {
        CreateMap<Message, MessageDto>();
        CreateMap<Message, MessageListDto>();

        CreateMap<SendMessageDto, Message>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            // Taken from CurrentUser.Id — a payload-supplied sender would allow impersonation.
            .ForMember(d => d.SenderId, o => o.Ignore())
            .ForMember(d => d.IsRead, o => o.Ignore())
            .ForMember(d => d.ReadDate, o => o.Ignore());
    }

    private void ConfigureVisit()
    {
        CreateMap<Visit, VisitDto>();
        CreateMap<Visit, VisitListDto>();

        CreateMap<CreateVisitDto, Visit>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore())
            // Nullable on the input (defaults to the caller), non-nullable on the entity.
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.IsCompleted, o => o.Ignore());

        CreateMap<UpdateVisitDto, Visit>()
            .IncludeBase<CreateVisitDto, Visit>()
            .ForMember(d => d.IsCompleted, o => o.MapFrom(s => s.IsCompleted));
    }

    private void ConfigureSupportTicket()
    {
        CreateMap<SupportTicket, SupportTicketDto>();
        CreateMap<SupportTicket, SupportTicketListDto>();
        CreateMap<SupportTicketMessage, SupportTicketMessageDto>();

        // CreateSupportTicketDto / AddSupportTicketMessageDto have no entity mapping on purpose:
        // both carry an opening message and both need the author taken from the session, so the
        // entities are built explicitly in SupportTicketAppService.
    }
}
