using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Reports.Dtos.Navigations;

/// <summary>
/// An activity report with its workplace and its typed data rows.
/// <para>
/// Class-typed properties are forbidden on ordinary DTOs, so combined reads live in a
/// <see cref="NavigationDto"/> derivative (see docs/ARCHITECTURE.md section 4).
/// </para>
/// </summary>
public class ActivityReportNavigationDto : NavigationDto
{
    public ActivityReportDto ActivityReport { get; set; } = null!;

    public LookupDto? Company { get; set; }

    /// <summary>Data rows in <c>OrderNo</c> order.</summary>
    public List<ActivityReportLineDto> Lines { get; set; } = [];
}

/// <summary>
/// A year-end review report with its workplace and its work items as a tree.
/// <para>
/// The repository builds the whole tree in a fixed number of queries regardless of depth, so
/// this view is assembled from what it returns rather than by walking parents one row at a time.
/// </para>
/// </summary>
public class YearEndReviewReportNavigationDto : NavigationDto
{
    public YearEndReviewReportDto YearEndReviewReport { get; set; } = null!;

    public LookupDto? Company { get; set; }

    /// <summary>Root-level work items; each carries its own subtree.</summary>
    public List<YearEndReviewLineNavigationDto> Activities { get; set; } = [];
}

/// <summary>One node of the year-end review work item tree.</summary>
public class YearEndReviewLineNavigationDto : NavigationDto
{
    public YearEndReviewLineDto Line { get; set; } = null!;

    /// <summary>Child work items of this node, in <c>OrderNo</c> order.</summary>
    public List<YearEndReviewLineNavigationDto> ChildActivities { get; set; } = [];
}
