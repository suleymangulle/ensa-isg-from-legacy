using Ensa.Domain.Common;
using Ensa.Domain.Communication;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Communication;

/// <summary>
/// EF Core implementation of <see cref="IMessageRepository"/>.
/// Tenant filtering comes from the global query filter.
/// </summary>
public class MessageRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<Message>(context, dataFilter), IMessageRepository
{
    /// <inheritdoc />
    /// <remarks>The count is computed <b>in the database</b>; the messages are not loaded into memory.</remarks>
    public Task<int> GetUnreadCountAsync(
        int userId,
        CancellationToken cancellationToken = default)
        => GetReadOnlyQueryable()
            .CountAsync(m => m.RecipientId == userId && !m.IsRead, cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// Paging is delegated to the base class <c>GetPagedListAsync</c>; the ordering is chronological on
    /// <c>CreationTime</c> (with <c>Id</c> as a secondary key for deterministic paging).
    /// </remarks>
    public Task<List<Message>> GetCorrespondenceAsync(
        int userId1,
        int userId2,
        int skipCount,
        int maxResultCount,
        CancellationToken cancellationToken = default)
        => GetPagedListAsync(
            skipCount,
            maxResultCount,
            sorting: $"{nameof(Message.CreationTime)} ASC, {nameof(Message.Id)} ASC",
            predicate: m => (m.SenderId == userId1 && m.RecipientId == userId2)
                            || (m.SenderId == userId2 && m.RecipientId == userId1),
            cancellationToken: cancellationToken);
}
