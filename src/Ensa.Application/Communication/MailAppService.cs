using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication;
using Ensa.Application.Contracts.Communication.Dtos;
using Ensa.Application.Contracts.Communication.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Communication;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Communication;

/// <summary>
/// Outbound mail queue application service.
/// <para>
/// <b>This service never talks to an SMTP server, deliberately.</b> Delivery is slow, fails in
/// ways that need retrying with backoff, and would either hold a database transaction open for
/// the duration of a mail-server timeout or leave a mail sent but unrecorded when the request is
/// aborted mid-flight. Sending therefore belongs to a background worker that polls
/// <see cref="GetPendingAsync"/>, uses the organization's <c>EmailSettings</c>, and reports the
/// outcome back through <see cref="MarkSentAsync"/> or <see cref="MarkFailedAsync"/>. This
/// service owns the queue and nothing else.
/// </para>
/// <para>
/// That worker is <c>MailDeliveryWorker</c> in the host. It does not call this service: a
/// worker has no user whose permissions can be checked and no single organization to belong to,
/// so it works through the repositories with the tenant filter disabled and resolves each
/// message's own account. See ADR-027.
/// </para>
/// </summary>
public class MailAppService(
    IServiceProvider serviceProvider,
    IMailRepository mailRepository,
    IRepository<MailAttachment> mailAttachmentRepository)
    : EnsaAppService(serviceProvider), IMailAppService
{
    /// <summary>Upper bound for one background-worker batch.</summary>
    private const int PendingMaxRecord = 200;

    /// <summary>Retry ceiling mirrored from <c>IMailRepository.GetPendingAsync</c>.</summary>
    private const int MaximumAttemptCount = 3;

    /// <inheritdoc />
    public async Task<MailDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Mail.Default);

        var mail = await mailRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Mail), id);

        return ObjectMapper.Map<Mail, MailDto>(mail);
    }

    /// <inheritdoc />
    public async Task<MailNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Mail.Default);

        var navigation = await mailRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(Mail), id);

        return new MailNavigationDto
        {
            Mail = ObjectMapper.Map<Mail, MailDto>(navigation.Mail),
            Attachments =
            [
                .. navigation.Attachments
                    .OrderBy(e => e.MailAttachment.OrderNo)
                    .Select(e => new MailAttachmentNavigationDto
                    {
                        Attachment = ObjectMapper.Map<MailAttachment, MailAttachmentDto>(e.MailAttachment),
                        Document = e.Document is null
                            ? null
                            : new LookupDto
                            {
                                Id = e.Document.Id,
                                DisplayName = e.Document.DocumentName,
                                Code = e.Document.Extension,
                                IsActive = e.Document.IsActive
                            }
                    })
            ]
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<MailListDto>> GetListAsync(
        GetMailListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Mail.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "CreationTime DESC");

        var total = await mailRepository.GetCountAsync(predicate, cancellationToken);

        var records = await mailRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Mail>, List<MailListDto>>(records);

        return new PagedResultDto<MailListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<MailDto> CreateAsync(CreateMailDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Mail.Create);

        var mail = ObjectMapper.Map<CreateMailDto, Mail>(input);
        mail.MailStatus = MailStatus.Draft;
        mail.AttemptCount = 0;

        mail = await mailRepository.InsertAsync(mail, autoSave: true, cancellationToken);

        Logger.LogInformation("Mail created as draft: {MailId} — {Topic}", mail.Id, mail.Topic);

        return ObjectMapper.Map<Mail, MailDto>(mail);
    }

    /// <inheritdoc />
    public async Task<MailDto> UpdateAsync(
        int id,
        UpdateMailDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Mail.Update);

        var mail = await mailRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Mail), id);

        EnsureNotSent(mail);

        ObjectMapper.Map(input, mail);

        mail = await mailRepository.UpdateAsync(mail, autoSave: true, cancellationToken);

        return ObjectMapper.Map<Mail, MailDto>(mail);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Mail.Delete);

        var mail = await mailRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Mail), id);

        var attachments = await mailAttachmentRepository.GetListAsync(e => e.MailId == id, cancellationToken);
        if (attachments.Count > 0)
        {
            await mailAttachmentRepository.DeleteManyAsync(attachments, autoSave: false, cancellationToken);
        }

        await mailRepository.DeleteAsync(mail, autoSave: true, cancellationToken);

        Logger.LogInformation("Mail deleted: {MailId}", id);
    }

    // ------------------------------------------------------------- Lifecycle

    /// <inheritdoc />
    public async Task<MailDto> QueueAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Mail.Update);

        var mail = await mailRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Mail), id);

        if (mail.MailStatus is not (MailStatus.Draft or MailStatus.Failed or MailStatus.Cancelled))
        {
            throw new BusinessException(
                    "Only a draft, failed or cancelled mail can be queued.",
                    "Ensa:Mail:NotQueueable")
                .WithData("Topic", mail.Topic)
                .WithData("MailStatus", mail.MailStatus);
        }

        mail.MailStatus = MailStatus.Queued;

        // The previous error is cleared but the attempt count is kept, so a mail that has already
        // burned its retries does not get a fresh budget just by being requeued.
        mail.ErrorMessage = null;

        mail = await mailRepository.UpdateAsync(mail, autoSave: true, cancellationToken);

        return ObjectMapper.Map<Mail, MailDto>(mail);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<MailDto>> GetPendingAsync(
        int maxResultCount,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Mail.Default);

        var batchSize = maxResultCount <= 0 ? 1 : Math.Min(maxResultCount, PendingMaxRecord);

        var records = await mailRepository.GetPendingAsync(
            batchSize,
            MaximumAttemptCount,
            cancellationToken);

        return new ListResultDto<MailDto>(ObjectMapper.Map<List<Mail>, List<MailDto>>(records));
    }

    /// <inheritdoc />
    public async Task<MailDto> MarkSentAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Mail.Update);

        var mail = await mailRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Mail), id);

        if (mail.MailStatus == MailStatus.Sent)
        {
            throw new BusinessException(
                    "The mail has already been marked as sent.",
                    "Ensa:Mail:AlreadySent")
                .WithData("Topic", mail.Topic);
        }

        mail.MailStatus = MailStatus.Sent;
        mail.SubmissionDate = Clock.Now;
        mail.AttemptCount += 1;
        mail.ErrorMessage = null;

        mail = await mailRepository.UpdateAsync(mail, autoSave: true, cancellationToken);

        Logger.LogInformation("Mail marked as sent: {MailId}", id);

        return ObjectMapper.Map<Mail, MailDto>(mail);
    }

    /// <inheritdoc />
    public async Task<MailDto> MarkFailedAsync(
        int id,
        string error,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        await CheckPermissionAsync(EnsaPermissions.Mail.Update);

        var mail = await mailRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Mail), id);

        if (mail.MailStatus == MailStatus.Sent)
        {
            throw new BusinessException(
                    "A mail that has already been sent cannot be marked as failed.",
                    "Ensa:Mail:AlreadySent")
                .WithData("Topic", mail.Topic);
        }

        mail.MailStatus = MailStatus.Failed;
        mail.ErrorMessage = error.Trim();
        mail.SubmissionDate = Clock.Now;
        mail.AttemptCount += 1;

        mail = await mailRepository.UpdateAsync(mail, autoSave: true, cancellationToken);

        Logger.LogWarning(
            "Mail delivery failed: {MailId} (attempt {AttemptCount}) — {Error}",
            id,
            mail.AttemptCount,
            mail.ErrorMessage);

        return ObjectMapper.Map<Mail, MailDto>(mail);
    }

    // ----------------------------------------------------------- Attachments

    /// <inheritdoc />
    public async Task<ListResultDto<MailAttachmentDto>> GetAttachmentsAsync(
        int mailId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Mail.Default);

        _ = await mailRepository.FindAsync(mailId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Mail), mailId);

        var attachments = await mailAttachmentRepository.GetListAsync(
            e => e.MailId == mailId,
            cancellationToken);

        var items = ObjectMapper
            .Map<List<MailAttachment>, List<MailAttachmentDto>>(attachments)
            .OrderBy(e => e.OrderNo)
            .ToList();

        return new ListResultDto<MailAttachmentDto>(items);
    }

    /// <inheritdoc />
    public async Task<MailAttachmentDto> AddAttachmentAsync(
        int mailId,
        AddMailAttachmentDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Mail.Update);

        var mail = await mailRepository.FindAsync(mailId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Mail), mailId);

        EnsureNotSent(mail);

        var existing = await mailAttachmentRepository.GetListAsync(
            e => e.MailId == mailId,
            cancellationToken);

        var attachment = new MailAttachment
        {
            MailId = mailId,
            DocumentId = input.DocumentId,
            OrderNo = input.OrderNo > 0
                ? input.OrderNo
                : existing.Count == 0 ? 1 : existing.Max(e => e.OrderNo) + 1
        };

        attachment = await mailAttachmentRepository.InsertAsync(attachment, autoSave: true, cancellationToken);

        return ObjectMapper.Map<MailAttachment, MailAttachmentDto>(attachment);
    }

    /// <inheritdoc />
    public async Task RemoveAttachmentAsync(
        int mailId,
        int attachmentId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Mail.Update);

        var mail = await mailRepository.FindAsync(mailId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(Mail), mailId);

        EnsureNotSent(mail);

        var attachment = await mailAttachmentRepository.FindAsync(attachmentId, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(MailAttachment), attachmentId);

        if (attachment.MailId != mailId)
        {
            throw new BusinessException(
                    "The attachment does not belong to this mail.",
                    "Ensa:Mail:AttachmentNotInMail")
                .WithData("Topic", mail.Topic);
        }

        await mailAttachmentRepository.DeleteAsync(attachment, autoSave: true, cancellationToken);
    }

    // -----------------------------------------------------------------

    /// <summary>
    /// Rejects edits to a mail that has already gone out. Rewriting the archived copy would make
    /// it disagree with what the recipient actually received.
    /// </summary>
    private static void EnsureNotSent(Mail mail)
    {
        if (mail.MailStatus == MailStatus.Sent)
        {
            throw new BusinessException(
                    "A mail that has already been sent can no longer be modified.",
                    "Ensa:Mail:AlreadySent")
                .WithData("Topic", mail.Topic);
        }
    }

    private static Expression<Func<Mail, bool>>? BuildFilter(GetMailListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var status = input.MailStatus;
        var mailType = input.MailType;
        var priority = input.MailPriority;
        var startDate = input.StartDate;
        var endDate = input.EndDate;

        if (search is null
            && status is null
            && mailType is null
            && priority is null
            && startDate is null
            && endDate is null)
        {
            return null;
        }

        return m =>
            (search == null
             || m.Topic.Contains(search)
             || m.Recipient.Contains(search)
             || m.Sender.Contains(search))
            && (status == null || m.MailStatus == status)
            && (mailType == null || m.MailType == mailType)
            && (priority == null || m.MailPriority == priority)
            && (startDate == null || m.CreationTime >= startDate)
            && (endDate == null || m.CreationTime <= endDate);
    }
}
