using Ensa.Domain.Repositories;
using Ensa.Domain.Risks.Navigations;

namespace Ensa.Domain.Risks;

/// <summary>Queries specific to corrective action records.</summary>
public interface ICorrectiveActionRepository : IRepository<CorrectiveAction>
{
    /// <summary>Loads the corrective action with its company, responsible employee, documents and source field observation line.</summary>
    Task<CorrectiveActionNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of the company's corrective actions still in <c>InProgress</c>, for the
    /// dashboard indicator.
    /// </summary>
    Task<int> GetOpenCorrectiveActionCountAsync(
        int companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists corrective actions whose deadline is earlier than <paramref name="reference"/> and that
    /// are still open.
    /// </summary>
    /// <param name="reference">Reference date; today by default.</param>
    /// <param name="companyId">Optional; narrows the result to a single company.</param>
    Task<List<CorrectiveAction>> GetDeadlineOverdueAsync(
        DateTime reference,
        int? companyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the corrective actions derived from a given field observation line.</summary>
    Task<List<CorrectiveAction>> GetByFieldObservationLineAsync(
        int fieldObservationLineId,
        CancellationToken cancellationToken = default);
}
