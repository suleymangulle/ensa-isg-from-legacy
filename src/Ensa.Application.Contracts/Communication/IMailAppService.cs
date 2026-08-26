using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication.Dtos;
using Ensa.Application.Contracts.Communication.Dtos.Navigations;

namespace Ensa.Application.Contracts.Communication;

/// <summary>
/// The outbound mail queue: composing, queueing, attaching files and recording delivery outcomes.
/// <para>
/// <b>This service does not send mail, and deliberately so.</b> SMTP delivery is slow, fails in
/// ways that need retrying with backoff, and must not run inside a request's transaction — a
/// timeout against a mail server would otherwise hold a database transaction open or, worse,
/// leave a mail sent but not recorded. Delivery is therefore the job of a background worker that
/// polls <see cref="GetPendingAsync"/>, talks to SMTP using the organization's
/// <c>EmailSettings</c>, and reports back through <see cref="MarkSentAsync"/> or
/// <see cref="MarkFailedAsync"/>.
/// </para>
/// <para>
/// That worker is <c>MailDeliveryWorker</c> in the host; it polls, sends, and reports back
/// through the two methods below. Nothing in <i>this</i> service moves a message on its own —
/// queueing and delivering are deliberately separate. See ADR-027.
/// </para>
/// </summary>
public interface IMailAppService : IApplicationService
{
    Task<MailDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The mail together with its attachments, resolved to file names.</summary>
    Task<MailNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<MailListDto>> GetListAsync(
        GetMailListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a mail as a draft. Call <see cref="QueueAsync"/> to hand it to the sender.</summary>
    Task<MailDto> CreateAsync(CreateMailDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits a mail. Only a draft or a failed mail may be edited — rewriting one that has already
    /// gone out would make the archive disagree with what the recipient received.
    /// </summary>
    Task<MailDto> UpdateAsync(int id, UpdateMailDto input, CancellationToken cancellationToken = default);

    /// <summary>Deletes the mail together with its attachment rows (soft delete).</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // -------------------------------------------------------------- Lifecycle

    /// <summary>
    /// Moves a draft or a failed mail into the queue so the background worker will pick it up.
    /// Requeueing a failed mail clears the previous error but keeps the attempt count, so the
    /// worker's retry ceiling still applies.
    /// </summary>
    Task<MailDto> QueueAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queued mails waiting to be sent, oldest first — the background worker's inbox.
    /// </summary>
    /// <param name="maxResultCount">Batch size. Clamped to a sane upper bound.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ListResultDto<MailDto>> GetPendingAsync(
        int maxResultCount,
        CancellationToken cancellationToken = default);

    /// <summary>Records a successful delivery. Reported by the background worker.</summary>
    Task<MailDto> MarkSentAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed delivery attempt: stores the error, stamps the attempt time and
    /// increments the attempt counter the worker uses to decide whether to retry.
    /// </summary>
    Task<MailDto> MarkFailedAsync(int id, string error, CancellationToken cancellationToken = default);

    // ------------------------------------------------------------ Attachments

    Task<ListResultDto<MailAttachmentDto>> GetAttachmentsAsync(
        int mailId,
        CancellationToken cancellationToken = default);

    /// <summary>Attaches a stored document. Refused once the mail has been sent.</summary>
    Task<MailAttachmentDto> AddAttachmentAsync(
        int mailId,
        AddMailAttachmentDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Detaches a file. Refused once the mail has been sent.</summary>
    Task RemoveAttachmentAsync(int mailId, int attachmentId, CancellationToken cancellationToken = default);
}
