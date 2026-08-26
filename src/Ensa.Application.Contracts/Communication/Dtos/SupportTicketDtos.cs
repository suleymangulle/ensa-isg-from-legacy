using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Communication.Dtos;

/// <summary>Support ticket list row.</summary>
public class SupportTicketListDto : EntityDto
{
    public string Topic { get; set; } = string.Empty;
    public int OpenedByUserId { get; set; }
    public int? ResponderUserId { get; set; }
    public SupportTicketStatus Status { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? ClosingDate { get; set; }
}

/// <summary>Support ticket detail view.</summary>
public class SupportTicketDto : CreationAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string Topic { get; set; } = string.Empty;

    /// <summary>The user who opened the ticket. Always the caller at create time.</summary>
    public int OpenedByUserId { get; set; }

    /// <summary>The support user who took the ticket, once someone has replied.</summary>
    public int? ResponderUserId { get; set; }

    public int? ClosedByUserId { get; set; }

    public SupportTicketStatus Status { get; set; }

    public DateTime? ClosingDate { get; set; }
}

/// <summary>A single message in a ticket thread.</summary>
public class SupportTicketMessageDto : CreationAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int SupportTicketId { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Author. Always the caller at post time; never supplied by the client.</summary>
    public int SenderUserId { get; set; }

    /// <summary>The counterpart the message is addressed to.</summary>
    public int FieldUserId { get; set; }

    public bool IsRead { get; set; }
}

/// <summary>
/// Ticket creation input. There is no opener field: the ticket is always opened by the caller.
/// </summary>
public class CreateSupportTicketDto
{
    [Required(ErrorMessage = "The subject is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string Topic { get; set; } = string.Empty;

    /// <summary>Optional opening message, posted into the thread together with the ticket.</summary>
    [MaxLength(4000)]
    public string? FirstMessage { get; set; }
}

/// <summary>
/// Ticket update input. The status is not writable here — use <c>CloseAsync</c> / <c>ReopenAsync</c>,
/// which also maintain the closing date and the closing user.
/// </summary>
public class UpdateSupportTicketDto
{
    [Required(ErrorMessage = "The subject is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string Topic { get; set; } = string.Empty;

    /// <summary>Reassigns the ticket to another support user.</summary>
    public int? ResponderUserId { get; set; }
}

/// <summary>Thread message input.</summary>
public class AddSupportTicketMessageDto
{
    [Required(ErrorMessage = "The message text is required.")]
    [MaxLength(4000)]
    public string Message { get; set; } = string.Empty;
}

/// <summary>Support ticket list filter.</summary>
public class GetSupportTicketListInput : PagedAndSortedFilterDto
{
    public SupportTicketStatus? Status { get; set; }
    public int? OpenedByUserId { get; set; }
    public int? ResponderUserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>When <c>true</c>, only the caller's own tickets are returned.</summary>
    public bool OnlyMine { get; set; }
}

/// <summary>Open ticket count of the caller.</summary>
public class OpenSupportTicketCountDto
{
    public int UserId { get; set; }
    public int OpenCount { get; set; }
}
