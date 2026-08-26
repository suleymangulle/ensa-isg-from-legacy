using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Communication.Dtos;

/// <summary>Mail queue list row.</summary>
public class MailListDto : EntityDto
{
    public string Sender { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public MailStatus MailStatus { get; set; }
    public MailPriority MailPriority { get; set; }
    public MailType MailType { get; set; }
    public DateTime? SubmissionDate { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreationTime { get; set; }
}

/// <summary>Mail detail view.</summary>
public class MailDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string Sender { get; set; } = string.Empty;

    /// <summary>Recipient address or addresses, semicolon separated.</summary>
    public string Recipient { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public ContentFormat ContentFormat { get; set; }
    public MailPriority MailPriority { get; set; }
    public MailType MailType { get; set; }
    public MailStatus MailStatus { get; set; }

    /// <summary>Error text recorded by the background sender after a failed attempt.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>When the mail was sent, or last attempted.</summary>
    public DateTime? SubmissionDate { get; set; }

    public int AttemptCount { get; set; }
}

/// <summary>A file attached to a mail.</summary>
public class MailAttachmentDto : CreationAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int MailId { get; set; }
    public int DocumentId { get; set; }
    public int OrderNo { get; set; }
}

/// <summary>Mail creation input. New mails always start as a draft.</summary>
public class CreateMailDto
{
    [Required(ErrorMessage = "The sender address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Email)]
    public string Sender { get; set; } = string.Empty;

    [Required(ErrorMessage = "At least one recipient is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string Recipient { get; set; } = string.Empty;

    [Required(ErrorMessage = "The subject is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.LongName)]
    public string Topic { get; set; } = string.Empty;

    [Required(ErrorMessage = "The body is required.")]
    public string Content { get; set; } = string.Empty;

    public ContentFormat ContentFormat { get; set; } = ContentFormat.PlainText;

    public MailPriority MailPriority { get; set; } = MailPriority.Normal;

    public MailType MailType { get; set; } = MailType.Normal;
}

/// <summary>
/// Mail update input. <c>MailStatus</c> is intentionally absent: the lifecycle is driven by
/// <c>QueueAsync</c>, <c>MarkSentAsync</c> and <c>MarkFailedAsync</c>, never by a raw write.
/// </summary>
public class UpdateMailDto : CreateMailDto;

/// <summary>Attachment input — the file must already exist in the central document store.</summary>
public class AddMailAttachmentDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A document must be selected.")]
    public int DocumentId { get; set; }

    /// <summary>Display order. Zero means "append to the end".</summary>
    public int OrderNo { get; set; }
}

/// <summary>Mail queue list filter.</summary>
public class GetMailListInput : PagedAndSortedFilterDto
{
    public MailStatus? MailStatus { get; set; }
    public MailType? MailType { get; set; }
    public MailPriority? MailPriority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>Failure report written by the background sender.</summary>
public class MarkMailFailedDto
{
    [Required(ErrorMessage = "The error text is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string Error { get; set; } = string.Empty;
}
