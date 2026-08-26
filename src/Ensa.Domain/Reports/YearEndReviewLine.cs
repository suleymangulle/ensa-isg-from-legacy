using Ensa.Domain.Common;

namespace Ensa.Domain.Reports;

/// <summary>
/// A single activity line within a <see cref="YearEndReviewReport"/>.
/// <para>Legacy equivalent: <c>YSDRLines_T</c> (file: <c>YearEndDegerlendirmeReports_T.cs</c>).</para>
/// <para>
/// NORMALIZATION: the legacy <c>ChildActivitiesJson</c> column, which held the child activities as
/// a JSON array, and the <c>[NotMapped] List&lt;YSDRLines_T&gt; ChildActivities</c> field that
/// accompanied it, were REMOVED and normalized into a self-referencing tree through
/// <see cref="ParentLineId"/>. The list of child activities no longer lives on the entity; it is
/// carried by <c>YearEndReviewLineNavigation.ChildActivities</c> in
/// <c>Navigations\YearEndReviewReportNavigation.cs</c>.
/// </para>
/// </summary>
public class YearEndReviewLine : FullAuditedTenantEntity, IActivatable
{
    /// <summary>(Legacy: <c>RaporId</c>) FK — no navigation property.</summary>
    public int YearEndReviewReportId { get; set; }

    public int OrderNo { get; set; }

    /// <summary>(Legacy: <c>Tarih</c> string → <c>DateTime?</c>)</summary>
    public DateTime? Date { get; set; }

    public string? Work { get; set; }

    /// <summary>(Legacy: <c>KisiveUnvan</c>)</summary>
    public string? PersonVeTitle { get; set; }

    public string? RepeatCount { get; set; }

    public string? UsedMethod { get; set; }

    /// <summary>(Legacy: <c>SonucveYorum</c>)</summary>
    public string? ResultVeComment { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The parent activity line in the tree. <c>null</c> means a root-level activity.
    /// (This is the normalization of the legacy <c>ChildActivitiesJson</c> — see the class XML doc.)
    /// FK — no navigation property.
    /// </summary>
    public int? ParentLineId { get; set; }
}
