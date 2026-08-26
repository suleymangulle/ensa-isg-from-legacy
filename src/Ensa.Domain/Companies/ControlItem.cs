using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Companies;

/// <summary>
/// Definition of a single item on a company's monthly checklist.
/// It is defined per organization; <see cref="CompanyCheckLine"/> references this definition.
/// <para>Legacy equivalent: <c>ControlItem_T</c>.</para>
/// <para>
/// NORMALISATION: the legacy <c>Period</c> field was an expression string in the form
/// "y1"/"a6". The expression was normalised through <see cref="PeriodId"/> (the host
/// <c>Period</c> table) and additionally broken out into <see cref="PeriodUnit"/> +
/// <see cref="PeriodValue"/> so that it can be used in calculations directly.
/// </para>
/// </summary>
public class ControlItem : FullAuditedTenantEntity, IActivatable, IHasSortOrder
{
    /// <summary>Name of the check item.</summary>
    public string ControlItemName { get; set; } = string.Empty;

    /// <summary>FK to the host <c>Period</c> definition table.</summary>
    public int? PeriodId { get; set; }

    /// <summary>Period unit (day/week/month/year). Legacy "y1" → <c>Year</c>.</summary>
    public PeriodUnit PeriodUnit { get; set; } = PeriodUnit.Year;

    /// <summary>Period multiplier. Legacy "y1" → 1, "a6" → 6.</summary>
    public int PeriodValue { get; set; } = 1;

    /// <summary>Display order in the list. (Legacy: <c>Sira</c>)</summary>
    public int SortOrder { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
