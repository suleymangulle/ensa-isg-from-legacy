using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Lookups.Dtos;

/// <summary>
/// A NACE occupation code entry for the activity picker.
/// <para>
/// Carries the hazard class alongside the code because the two are inseparable in practice:
/// picking an activity determines the workplace hazard class, which in turn drives mandatory
/// training hours, examination periods and specialist assignment rules. Returning it with the
/// lookup lets the form fill the hazard class in the same round trip and lets
/// <c>CompanyManager</c> validate the pair the caller sent.
/// </para>
/// </summary>
public class OccupationCodeLookupDto : LookupDto
{
    /// <summary>Hazard class derived from the activity under occupational safety law no. 6331.</summary>
    public HazardClass HazardClass { get; set; }

    /// <summary>Activity description; the same text as <c>DisplayName</c>, kept for clarity.</summary>
    public string Tag { get; set; } = string.Empty;
}

/// <summary>A period definition (e.g. "every six months") for recurring-task pickers.</summary>
public class PeriodLookupDto : LookupDto
{
    /// <summary>Numeric part of the period, e.g. 6 for "every six months".</summary>
    public int PeriodValue { get; set; }

    /// <summary>Unit the value is expressed in.</summary>
    public PeriodUnit PeriodUnit { get; set; }
}
