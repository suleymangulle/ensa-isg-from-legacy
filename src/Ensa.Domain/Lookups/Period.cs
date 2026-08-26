using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Definition of a recurring work/inspection period (e.g. "every 6 months", "annually").
/// <para>Legacy equivalent: <c>Period_T</c>.</para>
/// <para>
/// The legacy <c>PeriodExpression</c> column was free text holding short codes such as
/// "y1"/"a6"; that structure has been normalised into <see cref="PeriodUnit"/> (enum) plus
/// <see cref="PeriodValue"/> (int). The raw expression is still kept in
/// <see cref="PeriodExpression"/> for backward compatibility.
/// </para>
/// <para>Host-level (tenant-less) reference table.</para>
/// </summary>
public class Period : AuditedEntity
{
    /// <summary>Display name (e.g. "every 6 months").</summary>
    public string PeriodName { get; set; } = string.Empty;

    /// <summary>Period value (6 for "every 6 months"). (Legacy: <c>PeriyotDegeri</c>)</summary>
    public int PeriodValue { get; set; }

    /// <summary>
    /// Period unit. (Legacy: the <c>PeriyotExpression</c> prefix — "y" for year, "a" for month.)
    /// </summary>
    public PeriodUnit PeriodUnit { get; set; }

    /// <summary>Raw legacy expression, kept for backward compatibility. (Legacy: <c>PeriyotExpression</c>)</summary>
    public string? PeriodExpression { get; set; }
}
