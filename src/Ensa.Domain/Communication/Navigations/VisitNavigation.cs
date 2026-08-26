using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Membership;

namespace Ensa.Domain.Communication.Navigations;

/// <summary>
/// Combined view of a <see cref="Visit"/> with its company and user, for the calendar screen.
/// <para>
/// RULE: it is <c>[NotMapped]</c>, never a <c>DbSet</c>, and never added to <c>ModelBuilder</c>.
/// <c>IVisitRepository.GetCalendarAsync</c> populates it through an <c>IQueryable</c> join and
/// projection.
/// </para>
/// </summary>
[NotMapped]
public class VisitNavigation : NavigationEntity
{
    /// <summary>The mapped root entity.</summary>
    public Visit Visit { get; set; } = null!;

    public Company? Company { get; set; }

    public User? User { get; set; }
}
