using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication.Dtos;
using Ensa.Application.Contracts.Communication.Dtos.Navigations;

namespace Ensa.Application.Contracts.Communication;

/// <summary>
/// Support tickets users raise against the system, and the message threads on them.
/// <para>
/// The opener of a ticket and the author of every thread message are taken from
/// <c>CurrentUser.Id</c>, never from the payload.
/// </para>
/// </summary>
public interface ISupportTicketAppService : IApplicationService
{
    Task<SupportTicketDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The ticket with its opener, its responder and the full message thread.</summary>
    Task<SupportTicketNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<SupportTicketListDto>> GetListAsync(
        GetSupportTicketListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a ticket in the caller's name. When an opening message is supplied it is posted
    /// into the thread as the first message.
    /// </summary>
    Task<SupportTicketDto> CreateAsync(CreateSupportTicketDto input, CancellationToken cancellationToken = default);

    Task<SupportTicketDto> UpdateAsync(
        int id,
        UpdateSupportTicketDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the ticket together with its thread.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // --------------------------------------------------------------- Messages

    Task<ListResultDto<SupportTicketMessageDto>> GetMessagesAsync(
        int ticketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts a message into the thread. When somebody other than the opener replies first, that
    /// user is recorded as the responder and the ticket moves to <c>Answered</c> — which is what
    /// makes the open-ticket count meaningful without a separate assignment step. Posting into a
    /// closed ticket is refused; reopen it first.
    /// </summary>
    Task<SupportTicketMessageDto> AddMessageAsync(
        int ticketId,
        AddSupportTicketMessageDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Closes the ticket, stamping the closing time and the closing user.</summary>
    Task<SupportTicketDto> CloseAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Reopens a closed or cancelled ticket, clearing the closing stamps.</summary>
    Task<SupportTicketDto> ReopenAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Number of the caller's own tickets that are not yet closed.</summary>
    Task<OpenSupportTicketCountDto> GetOpenCountAsync(CancellationToken cancellationToken = default);
}
