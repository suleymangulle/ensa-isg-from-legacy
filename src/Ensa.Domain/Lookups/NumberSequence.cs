using Ensa.Domain.Common;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Document number counter (e.g. quote number, contract number) — holds the last number
/// issued per company and per document type.
/// <para>Legacy equivalent: <c>Number_T</c>.</para>
/// </summary>
public class NumberSequence : AuditedTenantEntity, IActivatable
{
    /// <summary>
    /// Owner of the series. Which entity that is depends on <see cref="Type"/>: document series
    /// are per company, invoice series are per office. <c>0</c> means the series is not scoped to
    /// one owner (for example an invoice raised without an office). FK — no navigation property.
    /// </summary>
    public int ScopeId { get; set; }

    /// <summary>
    /// Number type. Document series use the legacy codes ("TEKLIF", "SOZLESME"); invoice series
    /// use <c>INVOICE-{year}</c>, so each year restarts at 1. (Legacy: <c>Tur</c>)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Last number issued. (Legacy: <c>Numara</c>)</summary>
    public int LatestNumber { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
