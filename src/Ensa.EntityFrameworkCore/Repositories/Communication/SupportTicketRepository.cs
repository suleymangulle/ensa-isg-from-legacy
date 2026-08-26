using Ensa.Domain.Common;
using Ensa.Domain.Communication;
using Ensa.Domain.Communication.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Communication;

/// <summary>
/// EF Core implementation of <see cref="ISupportTicketRepository"/>.
/// Tenant filtering comes from the global query filter.
/// </summary>
public class SupportTicketRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<SupportTicket>(context, dataFilter), ISupportTicketRepository
{
    /// <inheritdoc />
    /// <remarks>The correspondence history is fetched with a single query in chronological order (2 queries in total).</remarks>
    public async Task<SupportTicketNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var request = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (request is null)
        {
            return null;
        }

        return new SupportTicketNavigation
        {
            SupportTicket = request,
            Messages = await Context.Set<SupportTicketMessage>()
                .AsNoTracking()
                .Where(m => m.SupportTicketId == id)
                .OrderBy(m => m.CreationTime)
                .ThenBy(m => m.Id)
                .ToListAsync(cancellationToken)
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// "Open" means a ticket that has not yet moved to <see cref="SupportTicketStatus.Closed"/> or
    /// <see cref="SupportTicketStatus.Cancelled"/>.
    /// The count is computed <b>in the database</b>.
    /// </remarks>
    public Task<int> GetOpenRequestCountAsync(
        int openedByUserId,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .CountAsync(
                t => t.OpenedByUserId == openedByUserId
                     && t.Status != SupportTicketStatus.Closed
                     && t.Status != SupportTicketStatus.Cancelled,
                cancellationToken);
}
