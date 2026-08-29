using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Communication;

/// <summary>
/// A visit — made or planned — by a user (OHS specialist or physician) to a company, and its
/// calendar entry.
/// <para>Legacy equivalent: <c>Visit_T</c>.</para>
/// <para>
/// The legacy <c>Options</c> (string) column was REMOVED — nowhere in the legacy code base was it
/// ever set to a real value or read; it was simply dead.
/// </para>
/// </summary>
public class Visit : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The company being visited. FK — no navigation property.</summary>
    public int CompanyId { get; set; }

    /// <summary>The user making or planning the visit. FK — no navigation property.</summary>
    public int UserId { get; set; }

    public DateTime VisitDate { get; set; }

    /// <summary>Start time, for the calendar view.</summary>
    public DateTime? Start { get; set; }

    /// <summary>End time, for the calendar view.</summary>
    public DateTime? End { get; set; }

    /// <summary>(Legacy: <c>IslemTuru</c> string)</summary>
    public VisitType OperationType { get; set; } = VisitType.Unspecified;

    public string? Description { get; set; }

    /// <summary>Display colour in the calendar, as a hex value.</summary>
    public string? Color { get; set; }

    public int? ScheduledWeek { get; set; }

    public int? ScheduledMonth { get; set; }

    public int? RegionCode { get; set; }

    /// <summary>
    /// Distance in km to the other company visited on the same day; used for route planning.
    /// (Legacy: <c>DigerFirmaUzaklik</c> <c>double?</c> → <c>decimal?</c>)
    /// </summary>
    public decimal? OtherCompanyDistanceKm { get; set; }

    /// <summary>Whether the visit actually took place. Added — not present in legacy — to separate planned from completed visits.</summary>
    public bool IsCompleted { get; set; }
}
