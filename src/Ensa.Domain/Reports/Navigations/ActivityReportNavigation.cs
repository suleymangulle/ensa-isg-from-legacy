using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Companies;

namespace Ensa.Domain.Reports.Navigations;

/// <summary>
/// Combined view of an <see cref="ActivityReport"/> with its company and data rows
/// (<see cref="ActivityReportLine"/>).
/// <para>
/// RULE: it is <c>[NotMapped]</c>, never a <c>DbSet</c>, and never added to <c>ModelBuilder</c>.
/// <c>IActivityReportRepository.GetWithNavigationAsync</c> populates it through an
/// <c>IQueryable</c> join and projection.
/// </para>
/// </summary>
[NotMapped]
public class ActivityReportNavigation : NavigationEntity
{
    /// <summary>The mapped root entity.</summary>
    public ActivityReport ActivityReport { get; set; } = null!;

    public Company? Company { get; set; }

    /// <summary>Data rows produced by the report engine, ordered by row number.</summary>
    public List<ActivityReportLine> Lines { get; set; } = [];
}
