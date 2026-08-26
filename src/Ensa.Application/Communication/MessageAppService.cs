using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication;
using Ensa.Application.Contracts.Communication.Dtos;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Communication;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Communication;

/// <summary>
/// In-app messaging application service.
/// <para>
/// <b>The sender is always the authenticated caller.</b> <see cref="SendAsync"/> reads the author
/// from <c>CurrentUser.Id</c> and the input DTO has no sender field at all, because a
/// client-supplied sender id would let any authenticated user post messages in a colleague's
/// name — and these messages are treated as an informal record of who told whom what.
/// </para>
/// <para>
/// Reads are scoped the same way: the inbox is "addressed to me", the sent folder is "written by
/// me", and neither accepts a user id from the client. A message the caller is neither party to
/// reads as not found rather than forbidden, so its existence is not disclosed.
/// </para>
/// </summary>
public class MessageAppService(
    IServiceProvider serviceProvider,
    IMessageRepository messageRepository)
    : EnsaAppService(serviceProvider), IMessageAppService
{
    /// <inheritdoc />
    public async Task<MessageDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Message.Default);

        var userId = GetRequiredUserId();

        var message = await messageRepository.FindAsync(id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Message), id);

        if (message.SenderId != userId && message.RecipientId != userId)
        {
            // Deliberately "not found" rather than "forbidden": otherwise the response would
            // confirm that a message with this id exists between two other people.
            throw new EntityNotFoundException(typeof(Message), id);
        }

        return ObjectMapper.Map<Message, MessageDto>(message);
    }

    /// <inheritdoc />
    public async Task<MessageDto> SendAsync(
        SendMessageDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Message.Create);

        var senderId = GetRequiredUserId();

        if (input.RecipientId == senderId)
        {
            throw new BusinessException(
                "You cannot send a message to yourself.",
                "Ensa:Message:SelfMessage");
        }

        var message = ObjectMapper.Map<SendMessageDto, Message>(input);

        // Taken from the session, never from the payload — see the class remarks.
        message.SenderId = senderId;
        message.IsRead = false;
        message.ReadDate = null;

        message = await messageRepository.InsertAsync(message, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Message sent: {MessageId} — from {SenderId} to {RecipientId}",
            message.Id,
            message.SenderId,
            message.RecipientId);

        return ObjectMapper.Map<Message, MessageDto>(message);
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<MessageListDto>> GetInboxAsync(
        GetMessageListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Message.Default);

        var userId = GetRequiredUserId();

        return await GetFolderAsync(input, userId, inbox: true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<MessageListDto>> GetSentAsync(
        GetMessageListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Message.Default);

        var userId = GetRequiredUserId();

        return await GetFolderAsync(input, userId, inbox: false, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UnreadMessageCountDto> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Message.Default);

        var userId = GetRequiredUserId();

        var count = await messageRepository.GetUnreadCountAsync(userId, cancellationToken);

        return new UnreadMessageCountDto
        {
            UserId = userId,
            UnreadCount = count
        };
    }

    /// <inheritdoc />
    public async Task<MessageDto> MarkReadAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Message.Update);

        var userId = GetRequiredUserId();

        var message = await messageRepository.FindAsync(id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Message), id);

        if (message.RecipientId != userId)
        {
            // Only the recipient can have read a message; letting the sender flip the flag would
            // turn the read receipt into a meaningless field.
            throw new BusinessException(
                "Only the recipient of a message can mark it as read.",
                "Ensa:Message:NotRecipient");
        }

        if (!message.IsRead)
        {
            message.IsRead = true;
            message.ReadDate = Clock.Now;

            message = await messageRepository.UpdateAsync(message, autoSave: true, cancellationToken);
        }

        return ObjectMapper.Map<Message, MessageDto>(message);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Message.Delete);

        var userId = GetRequiredUserId();

        var message = await messageRepository.FindAsync(id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Message), id);

        if (message.SenderId != userId)
        {
            throw new BusinessException(
                "Only the sender of a message can delete it.",
                "Ensa:Message:NotSender");
        }

        await messageRepository.DeleteAsync(message, autoSave: true, cancellationToken);
    }

    // -----------------------------------------------------------------

    private async Task<PagedResultDto<MessageListDto>> GetFolderAsync(
        GetMessageListInput input,
        int userId,
        bool inbox,
        CancellationToken cancellationToken)
    {
        var predicate = BuildFilter(input, userId, inbox);
        var sorting = NormalizeSorting(input.Sorting, "CreationTime DESC");

        var total = await messageRepository.GetCountAsync(predicate, cancellationToken);

        var records = await messageRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Message>, List<MessageListDto>>(records);

        return new PagedResultDto<MessageListDto>(total, items);
    }

    private static Expression<Func<Message, bool>> BuildFilter(
        GetMessageListInput input,
        int userId,
        bool inbox)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var messageType = input.MessageType;
        var companyId = input.CompanyId;
        var correspondentId = input.CorrespondentUserId;
        var isRead = input.IsRead;
        var startDate = input.StartDate;
        var endDate = input.EndDate;

        return m =>
            (inbox ? m.RecipientId == userId : m.SenderId == userId)
            && (correspondentId == null
                || (inbox ? m.SenderId == correspondentId : m.RecipientId == correspondentId))
            && (search == null || m.Content.Contains(search))
            && (messageType == null || m.MessageType == messageType)
            && (companyId == null || m.CompanyId == companyId)
            && (isRead == null || m.IsRead == isRead)
            && (startDate == null || m.CreationTime >= startDate)
            && (endDate == null || m.CreationTime <= endDate);
    }
}
