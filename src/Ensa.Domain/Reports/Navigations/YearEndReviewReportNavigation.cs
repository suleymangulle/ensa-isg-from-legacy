using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Companies;

namespace Ensa.Domain.Reports.Navigations;

/// <summary>
/// Combined view of a <see cref="YearEndReviewReport"/> with its company and the hierarchical tree
/// of activity lines.
/// <para>
/// RULE: it is <c>[NotMapped]</c>, never a <c>DbSet</c>, and never added to <c>ModelBuilder</c>.
/// <c>IYearEndReviewReportRepository.GetWithNavigationAsync</c> populates it through an
/// <c>IQueryable</c> join and projection. The tree is built recursively into
/// <see cref="YearEndReviewLineNavigation.ChildActivities"/>, starting from the root rows
/// (<c>ParentLineId == null</c>).
/// </para>
/// </summary>
[NotMapped]
public class YearEndReviewReportNavigation : NavigationEntity
{
    /// <summary>The mapped root entity.</summary>
    public YearEndReviewReport YearEndReviewReport { get; set; } = null!;

    public Company? Company { get; set; }

    /// <summary>Root-level activity lines (<c>ParentLineId == null</c>); each carries its own subtree.</summary>
    public List<YearEndReviewLineNavigation> Activities { get; set; } = [];
}

/// <summary>
/// The hierarchical (tree) view of a <see cref="YearEndReviewLine"/>, which replaces the legacy
/// <c>ChildActivitiesJson</c> column.
/// </summary>
[NotMapped]
public class YearEndReviewLineNavigation : NavigationEntity
{
    public YearEndReviewLine Line { get; set; } = null!;

    /// <summary>Child activities of this line. (Legacy: <c>ChildActivitiesJson</c>)</summary>
    public List<YearEndReviewLineNavigation> ChildActivities { get; set; } = [];
}
