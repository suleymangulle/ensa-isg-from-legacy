using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Communication.Dtos;

/// <summary>In-app message list row (inbox and sent folder alike).</summary>
public class MessageListDto : EntityDto
{
    public MessageType MessageType { get; set; }
    public string Content { get; set; } = string.Empty;
    public int SenderId { get; set; }
    public int RecipientId { get; set; }
    public int? CompanyId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadDate { get; set; }
    public DateTime CreationTime { get; set; }
}

/// <summary>In-app message detail view.</summary>
public class MessageDto : CreationAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public MessageType MessageType { get; set; }
    public string Content { get; set; } = string.Empty;

    /// <summary>Recipient user.</summary>
    public int RecipientId { get; set; }

    /// <summary>Sender user. Always the caller at send time; never supplied by the client.</summary>
    public int SenderId { get; set; }

    /// <summary>The workplace the message is about, when it is about one.</summary>
    public int? CompanyId { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadDate { get; set; }
}

/// <summary>
/// Send input.
/// <para>
/// There is deliberately no sender field. The sender is taken from <c>CurrentUser.Id</c> so a
/// caller cannot post a message in someone else's name — an impersonation hole that a
/// client-supplied sender id would open in an audit trail people rely on.
/// </para>
/// </summary>
public class SendMessageDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A recipient must be selected.")]
    public int RecipientId { get; set; }

    [Required(ErrorMessage = "The message text is required.")]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public MessageType MessageType { get; set; } = MessageType.UserMessage;

    public int? CompanyId { get; set; }
}

/// <summary>Inbox / sent folder filter.</summary>
public class GetMessageListInput : PagedAndSortedFilterDto
{
    public MessageType? MessageType { get; set; }
    public int? CompanyId { get; set; }

    /// <summary>The other party in the conversation, when filtering to one correspondent.</summary>
    public int? CorrespondentUserId { get; set; }

    public bool? IsRead { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>Unread message count of the caller.</summary>
public class UnreadMessageCountDto
{
    public int UserId { get; set; }
    public int UnreadCount { get; set; }
}
