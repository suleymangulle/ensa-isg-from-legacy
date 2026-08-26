using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication.Dtos;

namespace Ensa.Application.Contracts.Communication;

/// <summary>
/// In-app messaging between users.
/// <para>
/// <b>The sender is always the caller.</b> Every write takes the author from
/// <c>CurrentUser.Id</c> and never from the payload: a client-supplied sender id would let any
/// authenticated user post messages in a colleague's name, and these messages are used as an
/// informal record of who told whom what.
/// </para>
/// <para>
/// Reads are scoped to the caller in the same way — the inbox returns messages addressed to the
/// caller, the sent folder returns messages written by the caller, and neither accepts a user id
/// from the client.
/// </para>
/// </summary>
public interface IMessageAppService : IApplicationService
{
    /// <summary>
    /// One message. Readable only by its sender or its recipient; anybody else gets a
    /// not-found result rather than a permission error, so message existence is not leaked.
    /// </summary>
    Task<MessageDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Sends a message from the caller to the given recipient.</summary>
    Task<MessageDto> SendAsync(SendMessageDto input, CancellationToken cancellationToken = default);

    /// <summary>Messages addressed to the caller, newest first.</summary>
    Task<PagedResultDto<MessageListDto>> GetInboxAsync(
        GetMessageListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Messages written by the caller, newest first.</summary>
    Task<PagedResultDto<MessageListDto>> GetSentAsync(
        GetMessageListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Unread message count of the caller — the badge on the message icon.</summary>
    Task<UnreadMessageCountDto> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message read. Only the recipient may do this: a sender marking their own message
    /// read would corrupt the read receipt.
    /// </summary>
    Task<MessageDto> MarkReadAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Deletes a message. Only its sender may do this.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
