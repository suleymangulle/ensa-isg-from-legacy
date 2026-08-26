using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Communication;

/// <summary>
/// An e-mail sent through the system, or waiting to be sent.
/// <para>Legacy equivalent: <c>Mail_T</c>.</para>
/// <para>
/// NORMALIZATION: the legacy <c>BagliDocuments</c> column (a CSV string listing document ids and
/// names) was REMOVED and normalized into the <see cref="MailAttachment"/> child table.
/// </para>
/// </summary>
public class Mail : FullAuditedTenantEntity
{
    public string Sender { get; set; } = string.Empty;

    /// <summary>Recipient address(es), separated by semicolons.</summary>
    public string Recipient { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    /// <summary>(Legacy: <c>MailIcerigi</c>)</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>(Legacy: <c>Icerik_Format</c> string)</summary>
    public ContentFormat ContentFormat { get; set; } = ContentFormat.PlainText;

    public MailPriority MailPriority { get; set; } = MailPriority.Normal;

    public MailType MailType { get; set; } = MailType.Normal;

    public MailStatus MailStatus { get; set; } = MailStatus.Draft;

    /// <summary>Error message recorded when delivery fails.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>When the mail was sent, or last attempted.</summary>
    public DateTime? SubmissionDate { get; set; }

    /// <summary>Delivery attempt count, used by the background worker's retry logic. Not present in legacy.</summary>
    public int AttemptCount { get; set; }
}
