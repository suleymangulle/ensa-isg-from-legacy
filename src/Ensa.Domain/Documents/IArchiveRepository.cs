using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Documents;

/// <summary>
/// Module-specific repository contract for <see cref="Archive"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface IArchiveRepository : IRepository<Archive>
{
    /// <summary>
    /// Returns the archive records for the given module type and module record.
    /// <paramref name="month"/> and <paramref name="year"/> narrow the result further when supplied.
    /// </summary>
    Task<List<Archive>> GetByModuleAsync(
        DocumentOwnerType moduleType,
        int moduleId,
        int? month = null,
        int? year = null,
        CancellationToken cancellationToken = default);
}
