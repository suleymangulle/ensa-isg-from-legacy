using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Plans;

/// <summary>
/// Catalogue definition of an activity, document or revision that can be added to a work plan.
/// <para>Legacy equivalent: <c>Activity_T</c>.</para>
/// <para>
/// NORMALIZATION: the legacy <c>[NotMapped] List&lt;Document_T&gt; Documents</c> collection
/// navigation was removed from the entity and moved to
/// <see cref="Navigations.ActivityNavigation"/>. The free-text legacy <c>Period</c> column was
/// normalized into the <see cref="PeriodId"/> FK (<c>Ensa.Domain.Lookups.Period</c>).
/// </para>
/// </summary>
public class Activity : FullAuditedTenantEntity, IActivatable
{
    /// <summary>Parent activity in the hierarchy (self-referencing FK). There is NO navigation property.</summary>
    public int? ParentActivityId { get; set; }

    public string? ActivityCode { get; set; }

    public string ActivityName { get; set; } = string.Empty;

    public int? ActivityGroupId { get; set; }

    /// <summary>(Legacy: <c>Tur</c> string — "Aktivite"/"Doküman"/"Revizyon"/"Zorunlu Evraklar")</summary>
    public ActivityType ActivityType { get; set; } = ActivityType.Activity;

    /// <summary>(Legacy: <c>DefaultAktivite</c>)</summary>
    public bool DefaultActivity { get; set; }

    /// <summary>(Legacy: <c>DefaultAdet</c>)</summary>
    public int DefaultCount { get; set; }

    /// <summary>(Legacy: <c>DefaultBaslangicAyKaydirma</c>)</summary>
    public int DefaultStartMonthOffset { get; set; }

    /// <summary>(Legacy: <c>DefaultElemanSarti</c>)</summary>
    public int DefaultElementCondition { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Aktivitenin tekrar periyodu. (Legacy: <c>Periyot</c> serbest metni — normalize edildi.)
    /// </summary>
    public int? PeriodId { get; set; }

    /// <summary>
    /// Name of the table the activity relates to — a polymorphic reference, e.g. "RiskAnalizRaporu".
    /// (Legacy: <c>IliskiliTablo</c>)
    /// </summary>
    public string? RelatedTable { get; set; }

    /// <summary>Record id of the polymorphic reference. (Legacy: <c>IliskiId</c>)</summary>
    public int? RelationId { get; set; }

    /// <summary>Sort priority in listings. (Legacy: <c>Sira</c>)</summary>
    public int? OrderNo { get; set; }
}
