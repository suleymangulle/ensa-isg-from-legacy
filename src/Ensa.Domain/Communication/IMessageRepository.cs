using Ensa.Domain.Repositories;

namespace Ensa.Domain.Communication;

/// <summary>
/// Module-specific queries for <see cref="Message"/>.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IMessageRepository : IRepository<Message>
{
    /// <summary>Returns the number of unread messages addressed to a user.</summary>
    Task<int> GetUnreadCountAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the conversation between two users in chronological order, paged.</summary>
    Task<List<Message>> GetCorrespondenceAsync(
        int userId1,
        int userId2,
        int skipCount,
        int maxResultCount,
        CancellationToken cancellationToken = default);
}
