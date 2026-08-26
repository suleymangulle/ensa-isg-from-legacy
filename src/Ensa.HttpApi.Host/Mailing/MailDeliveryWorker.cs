using Ensa.Domain.Common;
using Ensa.Domain.Communication;
using Ensa.Domain.Documents;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;
using Microsoft.Extensions.Options;

namespace Ensa.HttpApi.Host.Mailing;

/// <summary>Configuration for <see cref="MailDeliveryWorker"/>.</summary>
public sealed class MailDeliveryOptions
{
    public const string SectionName = "MailDelivery";

    /// <summary>
    /// Whether the worker runs. It is on by default and harmless when no organization has
    /// configured an account: it finds nothing to send and does nothing.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Seconds between polls.</summary>
    public int PollSeconds { get; set; } = 30;

    /// <summary>Messages taken per poll, across all organizations.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>
    /// Attempts before a message is given up on. A transient outage recovers within a few
    /// polls; anything that survives three attempts needs a person, not another retry.
    /// </summary>
    public int MaxAttemptCount { get; set; } = 3;
}

/// <summary>
/// Delivers the queued mail.
/// <para>
/// <b>Why this does not go through <c>IMailAppService</c>.</b> That service is written for
/// requests: it checks permissions and runs inside the caller's tenant. A worker has neither a
/// user to check nor a single tenant to belong to, so it works through the repositories directly
/// with the tenant filter disabled — the same deliberate, narrow exception the sign-in path makes
/// (ADR-011) — and picks each message's own organization to find the account to send with. Making
/// the app service accommodate this would mean weakening the checks that protect it from humans.
/// </para>
/// <para>
/// <b>Why sending is not part of the request that queues the mail.</b> Delivery is slow, fails in
/// ways that need retrying with backoff, and would hold a database transaction open for the
/// length of a mail-server timeout. Worse, a request aborted mid-flight would leave a mail sent
/// but unrecorded — and a duplicate notification is worse than a late one.
/// </para>
/// </summary>
public sealed class MailDeliveryWorker(
    IServiceProvider services,
    IOptions<MailDeliveryOptions> options,
    ILogger<MailDeliveryWorker> logger)
    : BackgroundService
{
    private readonly MailDeliveryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Mail delivery worker is disabled by configuration.");
            return;
        }

        logger.LogInformation(
            "Mail delivery worker started; polling every {PollSeconds}s, batch {BatchSize}.",
            _options.PollSeconds, _options.BatchSize);

        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.PollSeconds));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                await DeliverBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // A failure in one poll must not end the worker for the lifetime of the process.
                logger.LogError(exception, "Mail delivery poll failed; the worker continues.");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task DeliverBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var mailRepository = provider.GetRequiredService<IMailRepository>();
        var attachmentRepository = provider.GetRequiredService<IRepository<MailAttachment>>();
        var documentRepository = provider.GetRequiredService<IDocumentRepository>();
        var settingsRepository = provider.GetRequiredService<IReadOnlyRepository<EmailSettings>>();
        var storage = provider.GetRequiredService<IDocumentStorage>();
        var sender = provider.GetRequiredService<IMailSender>();
        var dataFilter = provider.GetRequiredService<IDataFilter>();
        var clock = provider.GetRequiredService<IClock>();

        // The queue spans every organization; a worker belongs to none of them.
        using var _ = dataFilter.Disable<IMultiTenant>();

        var pending = await mailRepository.GetPendingAsync(
            _options.BatchSize, _options.MaxAttemptCount, cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        var accounts = await LoadAccountsAsync(settingsRepository, pending, cancellationToken);

        foreach (var mail in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!accounts.TryGetValue(mail.TenantId ?? 0, out var account))
            {
                // Not a delivery failure: the organization has not configured an account yet.
                // Counting it as an attempt would burn the retries a real outage needs.
                logger.LogWarning(
                    "Mail {MailId} cannot be sent: organization {TenantId} has no mail account.",
                    mail.Id, mail.TenantId);
                continue;
            }

            await DeliverOneAsync(
                mail, account, mailRepository, attachmentRepository, documentRepository,
                storage, sender, clock, cancellationToken);
        }
    }

    /// <summary>One query for the accounts of every organization present in the batch.</summary>
    private static async Task<Dictionary<int, EmailSettings>> LoadAccountsAsync(
        IReadOnlyRepository<EmailSettings> repository,
        List<Mail> pending,
        CancellationToken cancellationToken)
    {
        var tenantIds = pending.Select(mail => mail.TenantId).Distinct().ToList();

        var accounts = await repository.GetListAsync(
            settings => settings.IsActive && tenantIds.Contains(settings.TenantId),
            cancellationToken);

        return accounts
            .GroupBy(settings => settings.TenantId ?? 0)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(s => s.Id).First());
    }

    private async Task DeliverOneAsync(
        Mail mail,
        EmailSettings account,
        IMailRepository mailRepository,
        IRepository<MailAttachment> attachmentRepository,
        IDocumentRepository documentRepository,
        IDocumentStorage storage,
        IMailSender sender,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var opened = new List<Stream>();

        try
        {
            var attachments = await LoadAttachmentsAsync(
                mail.Id, attachmentRepository, documentRepository, storage, opened, cancellationToken);

            await sender.SendAsync(
                account,
                new OutgoingMail
                {
                    Sender = mail.Sender,
                    Recipient = mail.Recipient,
                    Subject = mail.Topic,
                    Body = mail.Content,
                    IsHtml = mail.ContentFormat == ContentFormat.Html,
                    IsHighPriority = mail.MailPriority == MailPriority.High,
                    Attachments = attachments,
                },
                cancellationToken);

            mail.MailStatus = MailStatus.Sent;
            mail.SubmissionDate = clock.Now;
            mail.ErrorMessage = null;
            mail.AttemptCount += 1;

            await mailRepository.UpdateAsync(mail, autoSave: true, cancellationToken);

            logger.LogInformation("Mail {MailId} sent after {AttemptCount} attempt(s).",
                mail.Id, mail.AttemptCount);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            mail.AttemptCount += 1;

            // The message is kept for a person to read; the exception type alone is not enough
            // to act on and the full stack does not belong in a column.
            mail.ErrorMessage = Truncate(exception.Message, 500);

            var giveUp = mail.AttemptCount >= _options.MaxAttemptCount;
            mail.MailStatus = giveUp ? MailStatus.Failed : MailStatus.Queued;

            await mailRepository.UpdateAsync(mail, autoSave: true, cancellationToken);

            logger.LogWarning(
                exception,
                "Mail {MailId} delivery failed on attempt {AttemptCount}; status is now {Status}.",
                mail.Id, mail.AttemptCount, mail.MailStatus);
        }
        finally
        {
            foreach (var stream in opened)
            {
                await stream.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Resolves the attachment documents and opens their payloads.
    /// <para>
    /// A document whose bytes are missing is skipped rather than allowed to fail the whole
    /// message: a notification without its attachment still tells the recipient something, and
    /// the gap is logged.
    /// </para>
    /// </summary>
    private async Task<List<MailAttachmentContent>> LoadAttachmentsAsync(
        int mailId,
        IRepository<MailAttachment> attachmentRepository,
        IDocumentRepository documentRepository,
        IDocumentStorage storage,
        List<Stream> opened,
        CancellationToken cancellationToken)
    {
        var links = await attachmentRepository.GetListAsync(
            attachment => attachment.MailId == mailId, cancellationToken);

        if (links.Count == 0)
        {
            return [];
        }

        var documentIds = links.Select(link => link.DocumentId).Distinct().ToList();

        var documents = await documentRepository.GetListAsync(
            document => documentIds.Contains(document.Id), cancellationToken);

        var result = new List<MailAttachmentContent>();

        foreach (var link in links.OrderBy(link => link.OrderNo))
        {
            var document = documents.Find(candidate => candidate.Id == link.DocumentId);
            if (document is null)
            {
                logger.LogWarning(
                    "Mail {MailId} references document {DocumentId}, which no longer exists.",
                    mailId, link.DocumentId);
                continue;
            }

            Stream? content = null;

            if (document.Content is { } inline)
            {
                content = new MemoryStream(inline, writable: false);
            }
            else if (document.StoragePath is { } path)
            {
                content = await storage.OpenAsync(path, cancellationToken);
            }

            if (content is null)
            {
                logger.LogWarning(
                    "Mail {MailId}: the payload of document {DocumentId} is missing; sending without it.",
                    mailId, document.Id);
                continue;
            }

            opened.Add(content);
            result.Add(new MailAttachmentContent(document.DocumentName, document.ContentType, content));
        }

        return result;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
