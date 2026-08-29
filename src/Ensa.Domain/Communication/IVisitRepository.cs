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
    /// <param name="officeIds">
    /// Restricts the calendar to visits whose workplace belongs to one of these offices. Empty or
    /// <c>null</c> means no office restriction. A visit has no office column of its own -- it is the
    /// workplace that belongs to an office, which is the relationship the legacy visit calendar
    /// joined on as well.
    /// </param>
    Task<List<VisitNavigation>> GetCalendarAsync(
        int? userId,
        DateTime start,
        DateTime end,
        IReadOnlyList<int>? officeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a company's most recent visit, if any.</summary>
    Task<Visit?> GetLatestVisitAsync(
        int companyId,
        CancellationToken cancellationToken = default);
}
