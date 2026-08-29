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

    /// <summary>
    /// What the legacy row actually says about when the work happened, kept verbatim.
    /// <para>
    /// (Legacy: <c>YSDRSatirlari_T.Tarih</c>.) That column is typed as a date and is not one. Of
    /// the 6,557 rows that have anything in it, most hold a period rather than a day
    /// ("01.01.2025 - 31.12.2025"), some hold a year, some hold a rule ("Her yeni ise giriste"),
    /// and some hold the HTML of a pasted table cell. <see cref="Date"/> is filled only when the
    /// text is a single unambiguous date; this keeps the rest, because "throughout 2025" is the
    /// answer the report is making and discarding it would leave the row blank.
    /// </para>
    /// <para>
    /// Stored as written, entities included: sanitising here would silently alter a record, and
    /// escaping belongs where it is rendered.
    /// </para>
    /// </summary>
    public string? DateText { get; set; }

    public string? Work { get; set; }

    /// <summary>(Legacy: <c>KisiveUnvan</c>)</summary>
    public string? PersonAndTitle { get; set; }

    public string? RepeatCount { get; set; }

    public string? UsedMethod { get; set; }

    /// <summary>(Legacy: <c>SonucveYorum</c>)</summary>
    public string? ResultAndComment { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The parent activity line in the tree. <c>null</c> means a root-level activity.
    /// (This is the normalization of the legacy <c>ChildActivitiesJson</c> — see the class XML doc.)
    /// FK — no navigation property.
    /// </summary>
    public int? ParentLineId { get; set; }
}
