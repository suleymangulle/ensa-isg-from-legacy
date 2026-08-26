using Ensa.Domain.Repositories;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Module-specific repository contract for <see cref="NumberSequence"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface INumberSequenceRepository : IRepository<NumberSequence>
{
    /// <summary>
    /// Produces the next number for the given scope and type and updates the counter record.
    /// <para>
    /// CONCURRENCY: the implementation must take a row lock (<c>UPDLOCK, ROWLOCK</c> on SQL
    /// Server, or the equivalent transaction plus locking read strategy in EF Core); otherwise
    /// concurrent requests can hand the same number to more than one record.
    /// </para>
    /// </summary>
    Task<int> GetNextNumberAsync(int scopeId, string type, CancellationToken cancellationToken = default);
}
