namespace Ensa.Domain.Communication;

/// <summary>One file to attach to an outgoing message.</summary>
/// <param name="FileName">Name the recipient sees.</param>
/// <param name="ContentType">MIME type, or <c>null</c> to let the transport decide.</param>
/// <param name="Content">The payload. The sender reads it; the caller disposes it.</param>
public sealed record MailAttachmentContent(string FileName, string? ContentType, Stream Content);

/// <summary>A message ready to hand to a transport.</summary>
public sealed class OutgoingMail
{
    public string Sender { get; init; } = string.Empty;

    /// <summary>One or more addresses, semicolon separated — the column stores them that way.</summary>
    public string Recipient { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public bool IsHtml { get; init; }

    public bool IsHighPriority { get; init; }

    public IReadOnlyList<MailAttachmentContent> Attachments { get; init; } = [];
}

/// <summary>
/// Delivers a message to an SMTP server.
/// <para>
/// The transport is abstracted so the queue can be exercised without a mail server, and so that
/// swapping SMTP for a provider API later touches one class. Implementations are expected to
/// throw on failure — the caller records the error and decides whether to retry, because only it
/// knows the attempt count.
/// </para>
/// </summary>
public interface IMailSender
{
    /// <summary>
    /// Sends one message using the given account.
    /// </summary>
    /// <param name="account">The organization's outgoing mail account.</param>
    /// <param name="mail">The message.</param>
    Task SendAsync(EmailSettings account, OutgoingMail mail, CancellationToken cancellationToken = default);
}
