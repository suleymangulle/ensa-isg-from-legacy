using Ensa.Domain.Communication.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Communication;

/// <summary>
/// Module-specific queries for <see cref="SupportTicket"/>.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface ISupportTicketRepository : IRepository<SupportTicket>
{
    /// <summary>Loads the support ticket together with its message history.</summary>
    Task<SupportTicketNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the number of tickets a user opened that are still open.</summary>
    Task<int> GetOpenRequestCountAsync(
        int openedByUserId,
        CancellationToken cancellationToken = default);
}
