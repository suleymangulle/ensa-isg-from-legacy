using Ensa.Domain.Communication.Navigations;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Communication;

/// <summary>
/// Module-specific queries for <see cref="Mail"/>.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IMailRepository : IRepository<Mail>
{
    /// <summary>Loads the mail together with its attachments and their document details.</summary>
    Task<MailNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns queued mails (<see cref="MailStatus.Queued"/>) that have not yet exceeded the
    /// maximum attempt count, oldest first, for the background delivery worker.
    /// </summary>
    Task<List<Mail>> GetPendingAsync(
        int maxResult,
        int maximumAttemptCount = 3,
        CancellationToken cancellationToken = default);
}
