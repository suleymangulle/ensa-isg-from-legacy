using Ensa.Domain.Communication.Navigations;
using Ensa.Domain.Repositories;

namespace Ensa.Domain.Communication;

/// <summary>
/// Module-specific queries for <see cref="Visit"/>.
/// The implementation lives under <c>Ensa.EntityFrameworkCore\Repositories</c>.
/// </summary>
public interface IVisitRepository : IRepository<Visit>
{
    /// <summary>
    /// Returns the visits in the given date range, together with company and user details, for
    /// the calendar screen. When <paramref name="userId"/> is omitted, every user is included.
    /// </summary>
    Task<List<VisitNavigation>> GetCalendarAsync(
        int? userId,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a company's most recent visit, if any.</summary>
    Task<Visit?> GetLatestVisitAsync(
        int companyId,
        CancellationToken cancellationToken = default);
}
