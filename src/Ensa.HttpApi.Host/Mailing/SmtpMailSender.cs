using System.Net;
using System.Net.Mail;
using Ensa.Domain.Communication;

namespace Ensa.HttpApi.Host.Mailing;

/// <summary>
/// Delivers mail over SMTP with the account configured for the organization.
/// <para>
/// <b>Why <see cref="SmtpClient"/>?</b> It is part of the framework, and this application needs
/// exactly what it does: submit a message to a server over SMTP with STARTTLS and a password.
/// The richer clients in the ecosystem exist for IMAP, OAuth flows and protocol corners none of
/// which are used here, and every dependency in a product that has to be maintained for years is
/// a cost. <see cref="IMailSender"/> is the seam if that judgement ever changes.
/// </para>
/// <para>
/// Failures are thrown rather than swallowed: the caller owns the attempt count and the retry
/// decision, and a transport that quietly reported success would let a mail vanish.
/// </para>
/// </summary>
public sealed class SmtpMailSender(ILogger<SmtpMailSender> logger) : IMailSender
{
    /// <summary>
    /// Address separators the <c>Recipient</c> column may contain. Legacy stored several
    /// addresses in one string and both characters appear in the data.
    /// </summary>
    private static readonly char[] RecipientSeparators = [';', ','];

    /// <inheritdoc />
    public async Task SendAsync(
        EmailSettings account,
        OutgoingMail mail,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(mail);

        var recipients = mail.Recipient
            .Split(RecipientSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (recipients.Count == 0)
        {
            throw new InvalidOperationException("The message has no recipient.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(string.IsNullOrWhiteSpace(mail.Sender) ? account.Email : mail.Sender),
            Subject = mail.Subject,
            Body = mail.Body,
            IsBodyHtml = mail.IsHtml,
            Priority = mail.IsHighPriority ? MailPriority.High : MailPriority.Normal,
        };

        foreach (var recipient in recipients)
        {
            message.To.Add(recipient);
        }

        foreach (var attachment in mail.Attachments)
        {
            // Attachment does not take ownership in a way we can rely on across framework
            // versions, so the stream is left to the caller and only read here.
            message.Attachments.Add(new Attachment(
                attachment.Content,
                attachment.FileName,
                attachment.ContentType ?? "application/octet-stream"));
        }

        using var client = new SmtpClient(account.SmtpServer, account.Port)
        {
            EnableSsl = account.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(account.Email, account.Password),
        };

        logger.LogDebug(
            "Sending mail via {SmtpServer}:{Port} (ssl={Ssl}) to {RecipientCount} recipient(s)",
            account.SmtpServer, account.Port, account.UseSsl, recipients.Count);

        await client.SendMailAsync(message, cancellationToken);
    }
}
