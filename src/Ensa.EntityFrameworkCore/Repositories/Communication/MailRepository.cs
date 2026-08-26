using Ensa.Domain.Common;
using Ensa.Domain.Documents;
using Ensa.Domain.Communication;
using Ensa.Domain.Communication.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Communication;

/// <summary>
/// EF Core implementation of <see cref="IMailRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class MailRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<Mail>(context, dataFilter), IMailRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// <b>N+1 PREVENTION:</b> the document records of the attachments are fetched in a single query
    /// with <c>Contains</c> rather than per attachment, and matched up in memory (3 queries in total).
    /// </remarks>
    public async Task<MailNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var mail = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (mail is null)
        {
            return null;
        }

        var navigation = new MailNavigation { Mail = mail };

        var attachments = await Context.Set<MailAttachment>()
            .AsNoTracking()
            .Where(e => e.MailId == id)
            .OrderBy(e => e.OrderNo)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);

        var documentIds = attachments.ConvertAll(e => e.DocumentId).Distinct().ToList();

        List<Document> documents = documentIds.Count == 0
            ? []
            : await Context.Set<Document>()
                .AsNoTracking()
                .Where(d => documentIds.Contains(d.Id))
                .ToListAsync(cancellationToken);

        navigation.Attachments = attachments.ConvertAll(ek => new MailAttachmentNavigation
        {
            MailAttachment = ek,
            Document = documents.Find(d => d.Id == ek.DocumentId)
        });

        return navigation;
    }

    /// <inheritdoc />
    /// <remarks>
    /// This is a queue query: the oldest record comes first and the result set is capped
    /// <b>in the database</b> with <c>Take</c> — the table is never loaded into memory.
    /// </remarks>
    public Task<List<Mail>> GetPendingAsync(
        int maxResult,
        int maximumAttemptCount = 3,
        CancellationToken cancellationToken = default)
    {
        var takeCount = Math.Clamp(maxResult, 1, 1000);

        return GetReadOnlyQueryable()
            .Where(m => m.MailStatus == MailStatus.Queued
                        && m.AttemptCount < maximumAttemptCount)
            .OrderBy(m => m.CreationTime)
            .ThenBy(m => m.Id)
            .Take(takeCount)
            .ToListAsync(cancellationToken);
    }
}
