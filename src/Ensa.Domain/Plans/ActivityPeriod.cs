using Ensa.Domain.Common;

namespace Ensa.Domain.Plans;

/// <summary>
/// Link table mapping an activity to a period definition.
/// <para>Legacy equivalent: <c>ActivityPeriod_T</c>.</para>
/// </summary>
public class ActivityPeriod : CreationAuditedTenantEntity
{
    /// <summary>(<c>Ensa.Domain.Lookups.Period</c>'a FK.)</summary>
    public int PeriodId { get; set; }

    public int ActivityId { get; set; }
}
