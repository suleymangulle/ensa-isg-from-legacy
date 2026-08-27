using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication;
using Ensa.Application.Contracts.Communication.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// In-app messaging endpoints — <c>api/message</c>.
/// <para>
/// No route takes a sender or an owner id. Both the author of a message and the folder being
/// read are derived from the access token, so one user cannot post as another or read another
/// user's inbox — see <see cref="IMessageAppService"/>.
/// </para>
/// </summary>
public class MessageController(IMessageAppService messageAppService) : EnsaController
{
    /// <summary>Returns a single message the caller is a party to.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<MessageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MessageDto> GetAsync(int id, CancellationToken cancellationToken)
        => messageAppService.GetAsync(id, cancellationToken);

    /// <summary>Sends a message from the authenticated caller.</summary>
    [HttpPost]
    [ProducesResponseType<MessageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<MessageDto> SendAsync(
        [FromBody] SendMessageDto input,
        CancellationToken cancellationToken)
        => messageAppService.SendAsync(input, cancellationToken);

    /// <summary>Messages addressed to the caller.</summary>
    [HttpGet("inbox")]
    [ProducesResponseType<PagedResultDto<MessageListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<MessageListDto>> GetInboxAsync(
        [FromQuery] GetMessageListInput input,
        CancellationToken cancellationToken)
        => messageAppService.GetInboxAsync(input, cancellationToken);

    /// <summary>Messages written by the caller.</summary>
    [HttpGet("sent")]
    [ProducesResponseType<PagedResultDto<MessageListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<MessageListDto>> GetSentAsync(
        [FromQuery] GetMessageListInput input,
        CancellationToken cancellationToken)
        => messageAppService.GetSentAsync(input, cancellationToken);

    /// <summary>Unread message count of the caller.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType<UnreadMessageCountDto>(StatusCodes.Status200OK)]
    public Task<UnreadMessageCountDto> GetUnreadCountAsync(CancellationToken cancellationToken)
        => messageAppService.GetUnreadCountAsync(cancellationToken);

    /// <summary>Marks a message read. Only the recipient may do this.</summary>
    [HttpPost("{id:int}/read")]
    [ProducesResponseType<MessageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MessageDto> MarkReadAsync(int id, CancellationToken cancellationToken)
        => messageAppService.MarkReadAsync(id, cancellationToken);

    /// <summary>Deletes a message. Only its sender may do this.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => messageAppService.DeleteAsync(id, cancellationToken);
}
