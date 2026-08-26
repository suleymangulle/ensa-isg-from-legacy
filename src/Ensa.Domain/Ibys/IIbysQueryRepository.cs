using Ensa.Domain.Ibys.Navigations;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Ibys;

/// <summary>
/// Queries specific to IBYS submission records.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IIbysQueryRepository : IRepository<IbysQuery>
{
    /// <summary>
    /// Loads the query together with its workplace, employee and related examination forms.
    /// Returns <c>null</c> when it does not exist.
    /// </summary>
    Task<IbysQueryNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the records still awaiting a result from IBYS (sent but neither approved nor
    /// failed yet) — for the background status polling job.
    /// </summary>
    /// <param name="type">Query type filter.</param>
    /// <param name="maxResultCount">Maximum number of records to return.</param>
    Task<List<IbysQuery>> GetPendingAsync(
        IbysQueryType type,
        int maxResultCount = 100,
        CancellationToken cancellationToken = default);

    /// <summary>Finds a record by its IBYS query number (used to match up service callbacks).</summary>
    Task<IbysQuery?> FindByQueryNoAsync(
        string queryNo,
        CancellationToken cancellationToken = default);

    /// <summary>Loads every query belonging to the same group (bulk batch).</summary>
    Task<List<IbysQuery>> GetByGroupIdAsync(
        string groupId,
        CancellationToken cancellationToken = default);
}
