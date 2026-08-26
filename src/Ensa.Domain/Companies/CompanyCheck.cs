using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Companies;

/// <summary>
/// Header of a company's checklist for a given month.
/// <para>Legacy equivalent: <c>CompanyControlItem_T</c>.</para>
/// </summary>
public class CompanyCheck : FullAuditedTenantEntity, ICompanyScoped
{
    public int CompanyId { get; set; }

    /// <summary>The month the check belongs to — always stored as the first day of that month.</summary>
    public DateTime CheckMonth { get; set; }

    /// <summary>The date the check was actually carried out.</summary>
    public DateTime? ControlItemDate { get; set; }

    /// <summary>(Legacy: <c>Durum</c>, a string such as "Aktif")</summary>
    public CompanyCheckStatus Status { get; set; } = CompanyCheckStatus.Active;

    /// <summary>Scanned copy of the check form — FK to the central <c>Document</c> table.</summary>
    public int? DocumentId { get; set; }
}
