using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication;
using Ensa.Application.Contracts.Communication.Dtos;
using Ensa.Application.Contracts.Communication.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Communication;
using Ensa.Domain.Membership;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Communication;

/// <summary>
/// Support ticket application service.
/// <para>
/// The opener of a ticket and the author of every thread message come from
/// <c>CurrentUser.Id</c>, never from the request payload, so the thread stays a truthful record
/// of who said what.
/// </para>
/// </summary>
public class SupportTicketAppService(
    IServiceProvider serviceProvider,
    ISupportTicketRepository supportTicketRepository,
    IRepository<SupportTicketMessage> supportTicketMessageRepository,
    IUserRepository userRepository)
    : EnsaAppService(serviceProvider), ISupportTicketAppService
{
    /// <inheritdoc />
    public async Task<SupportTicketDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.SupportTicket.Default);

        var ticket = await supportTicketRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(SupportTicket), id);

        return ObjectMapper.Map<SupportTicket, SupportTicketDto>(ticket);
    }

    /// <inheritdoc />
    public async Task<SupportTicketNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.SupportTicket.Default);

        var navigation = await supportTicketRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(SupportTicket), id);

        var ticket = navigation.SupportTicket;

        // SupportTicketNavigation carries the thread but not the people, so the two participants
        // are resolved here. That is two extra point reads on a detail screen — cheap, and it
        // keeps the repository's projection focused on the thread itself.
        var openedBy = await userRepository.FindAsync(ticket.OpenedByUserId, cancellationToken);

        var responder = ticket.ResponderUserId is { } responderId
            ? await userRepository.FindAsync(responderId, cancellationToken)
            : null;

        return new SupportTicketNavigationDto
        {
            SupportTicket = ObjectMapper.Map<SupportTicket, SupportTicketDto>(ticket),
            OpenedByUser = ToLookup(openedBy),
            ResponderUser = ToLookup(responder),
            Messages =
            [
                .. ObjectMapper
                    .Map<List<SupportTicketMessage>, List<SupportTicketMessageDto>>(navigation.Messages)
                    .OrderBy(m => m.CreationTime)
                    .ThenBy(m => m.Id)
            ]
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<SupportTicketListDto>> GetListAsync(
        GetSupportTicketListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.SupportTicket.Default);

        var predicate = BuildFilter(input, input.OnlyMine ? GetRequiredUserId() : null);
        var sorting = NormalizeSorting(input.Sorting, "CreationTime DESC");

        var total = await supportTicketRepository.GetCountAsync(predicate, cancellationToken);

        var records = await supportTicketRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<SupportTicket>, List<SupportTicketListDto>>(records);

        return new PagedResultDto<SupportTicketListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<SupportTicketDto> CreateAsync(
        CreateSupportTicketDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.SupportTicket.Create);

        var userId = GetRequiredUserId();

        var ticket = new SupportTicket
        {
            Topic = input.Topic.Trim(),
            OpenedByUserId = userId,
            Status = SupportTicketStatus.Open
        };

        ticket = await supportTicketRepository.InsertAsync(ticket, autoSave: true, cancellationToken);

        if (!string.IsNullOrWhiteSpace(input.FirstMessage))
        {
            var firstMessage = new SupportTicketMessage
            {
                SupportTicketId = ticket.Id,
                Message = input.FirstMessage.Trim(),
                SenderUserId = userId,
                // Nobody is handling the ticket yet, so the message is addressed to the desk
                // rather than to a person; the opener stands in until a responder picks it up.
                FieldUserId = userId,
                IsRead = false
            };

            await supportTicketMessageRepository.InsertAsync(firstMessage, autoSave: true, cancellationToken);
        }

        Logger.LogInformation(
            "Support ticket opened: {TicketId} — {Topic} (by {UserId})",
            ticket.Id,
            ticket.Topic,
            userId);

        return ObjectMapper.Map<SupportTicket, SupportTicketDto>(ticket);
    }

    /// <inheritdoc />
    public async Task<SupportTicketDto> UpdateAsync(
        int id,
        UpdateSupportTicketDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.SupportTicket.Update);

        var ticket = await supportTicketRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(SupportTicket), id);

        ticket.Topic = input.Topic.Trim();

        if (input.ResponderUserId is { } responderId)
        {
            ticket.ResponderUserId = responderId;
        }

        ticket = await supportTicketRepository.UpdateAsync(ticket, autoSave: true, cancellationToken);

        return ObjectMapper.Map<SupportTicket, SupportTicketDto>(ticket);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.SupportTicket.Delete);

        var ticket = await supportTicketRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(SupportTicket), id);

        var messages = await supportTicketMessageRepository.GetListAsync(
            m => m.SupportTicketId == id,
            cancellationToken);

        if (messages.Count > 0)
        {
            await supportTicketMessageRepository.DeleteManyAsync(messages, autoSave: false, cancellationToken);
        }

        await supportTicketRepository.DeleteAsync(ticket, autoSave: true, cancellationToken);

        Logger.LogInformation("Support ticket deleted: {TicketId}", id);
    }

    // --------------------------------------------------------------- Messages

    /// <inheritdoc />
    public async Task<ListResultDto<SupportTicketMessageDto>> GetMessagesAsync(
        int ticketId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.SupportTicket.Default);

        _ = await supportTicketRepository.FindAsync(ticketId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(SupportTicket), ticketId);

        var messages = await supportTicketMessageRepository.GetListAsync(
            m => m.SupportTicketId == ticketId,
            cancellationToken);

        var items = ObjectMapper
            .Map<List<SupportTicketMessage>, List<SupportTicketMessageDto>>(messages)
            .OrderBy(m => m.CreationTime)
            .ThenBy(m => m.Id)
            .ToList();

        return new ListResultDto<SupportTicketMessageDto>(items);
    }

    /// <inheritdoc />
    public async Task<SupportTicketMessageDto> AddMessageAsync(
        int ticketId,
        AddSupportTicketMessageDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.SupportTicket.Create);

        var userId = GetRequiredUserId();

        var ticket = await supportTicketRepository.FindAsync(ticketId, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(SupportTicket), ticketId);

        if (ticket.Status == SupportTicketStatus.Closed)
        {
            throw new BusinessException(
                    "A closed ticket cannot receive new messages. Reopen it first.",
                    "Ensa:SupportTicket:MessageOnClosedTicket")
                .WithData("Topic", ticket.Topic);
        }

        var isOpener = userId == ticket.OpenedByUserId;

        var message = new SupportTicketMessage
        {
            SupportTicketId = ticketId,
            Message = input.Message.Trim(),
            SenderUserId = userId,
            // The counterpart: the opener writes to whoever is handling the ticket, support
            // writes back to the opener.
            FieldUserId = isOpener
                ? ticket.ResponderUserId ?? ticket.OpenedByUserId
                : ticket.OpenedByUserId,
            IsRead = false
        };

        message = await supportTicketMessageRepository.InsertAsync(message, autoSave: true, cancellationToken);

        // The first reply from somebody other than the opener is what assigns the ticket. Without
        // this the open-ticket count would keep counting tickets that support is already on.
        if (!isOpener)
        {
            ticket.ResponderUserId ??= userId;

            if (ticket.Status == SupportTicketStatus.Open)
            {
                ticket.Status = SupportTicketStatus.Answered;
            }

            await supportTicketRepository.UpdateAsync(ticket, autoSave: true, cancellationToken);
        }

        return ObjectMapper.Map<SupportTicketMessage, SupportTicketMessageDto>(message);
    }

    /// <inheritdoc />
    public async Task<SupportTicketDto> CloseAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.SupportTicket.Update);

        var ticket = await supportTicketRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(SupportTicket), id);

        if (ticket.Status == SupportTicketStatus.Closed)
        {
            throw new BusinessException(
                    "The ticket is already closed.",
                    "Ensa:SupportTicket:AlreadyClosed")
                .WithData("Topic", ticket.Topic);
        }

        ticket.Status = SupportTicketStatus.Closed;
        ticket.ClosingDate = Clock.Now;
        ticket.ClosedByUserId = GetRequiredUserId();

        ticket = await supportTicketRepository.UpdateAsync(ticket, autoSave: true, cancellationToken);

        Logger.LogInformation("Support ticket closed: {TicketId}", id);

        return ObjectMapper.Map<SupportTicket, SupportTicketDto>(ticket);
    }

    /// <inheritdoc />
    public async Task<SupportTicketDto> ReopenAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.SupportTicket.Update);

        var ticket = await supportTicketRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(SupportTicket), id);

        if (ticket.Status is not (SupportTicketStatus.Closed or SupportTicketStatus.Cancelled))
        {
            throw new BusinessException(
                    "Only a closed or cancelled ticket can be reopened.",
                    "Ensa:SupportTicket:NotClosed")
                .WithData("Topic", ticket.Topic);
        }

        ticket.Status = ticket.ResponderUserId is null
            ? SupportTicketStatus.Open
            : SupportTicketStatus.Answered;

        ticket.ClosingDate = null;
        ticket.ClosedByUserId = null;

        ticket = await supportTicketRepository.UpdateAsync(ticket, autoSave: true, cancellationToken);

        Logger.LogInformation("Support ticket reopened: {TicketId}", id);

        return ObjectMapper.Map<SupportTicket, SupportTicketDto>(ticket);
    }

    /// <inheritdoc />
    public async Task<OpenSupportTicketCountDto> GetOpenCountAsync(CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.SupportTicket.Default);

        var userId = GetRequiredUserId();

        var count = await supportTicketRepository.GetOpenRequestCountAsync(userId, cancellationToken);

        return new OpenSupportTicketCountDto
        {
            UserId = userId,
            OpenCount = count
        };
    }

    // -----------------------------------------------------------------

    private static LookupDto? ToLookup(User? user)
        => user is null
            ? null
            : new LookupDto
            {
                Id = user.Id,
                DisplayName = user.FullName,
                Code = user.UserName,
                IsActive = user.IsActive
            };

    private static Expression<Func<SupportTicket, bool>>? BuildFilter(
        GetSupportTicketListInput input,
        int? onlyForUserId)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var status = input.Status;
        var openedBy = input.OpenedByUserId;
        var responder = input.ResponderUserId;
        var startDate = input.StartDate;
        var endDate = input.EndDate;

        if (search is null
            && status is null
            && openedBy is null
            && responder is null
            && startDate is null
            && endDate is null
            && onlyForUserId is null)
        {
            return null;
        }

        return t =>
            (onlyForUserId == null || t.OpenedByUserId == onlyForUserId)
            && (search == null || t.Topic.Contains(search))
            && (status == null || t.Status == status)
            && (openedBy == null || t.OpenedByUserId == openedBy)
            && (responder == null || t.ResponderUserId == responder)
            && (startDate == null || t.CreationTime >= startDate)
            && (endDate == null || t.CreationTime <= endDate);
    }
}
