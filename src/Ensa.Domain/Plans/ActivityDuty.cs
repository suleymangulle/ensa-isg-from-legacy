using Ensa.Domain.Common;

namespace Ensa.Domain.Plans;

/// <summary>
/// Link table mapping an activity to the duty or job title responsible for it.
/// <para>Legacy equivalent: <c>ActivityDuty_T</c>.</para>
/// <para>
/// Legacy stored only the free-text <c>DutyCode</c>. The <see cref="DutyId"/> FK was added so the
/// value can be normalized against the <c>Ensa.Domain.Lookups.Duty</c> reference table.
/// <see cref="DutyCode"/> is kept alongside it so that no data is lost until the mapping is
/// complete.
/// </para>
/// </summary>
public class ActivityDuty : AuditedTenantEntity
{
    public int ActivityId { get; set; }

    /// <summary>The legacy free-text duty code, kept for backward compatibility.</summary>
    public string? DutyCode { get; set; }

    /// <summary>Normalized duty reference (<c>Ensa.Domain.Lookups.Duty</c>). (NEW field)</summary>
    public int? DutyId { get; set; }
}
