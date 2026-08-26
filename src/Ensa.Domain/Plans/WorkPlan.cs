using Ensa.Domain.Common;

namespace Ensa.Domain.Plans;

/// <summary>
/// The header — the cover page — of a company's annual occupational health and safety work plan.
/// <para>Legacy equivalent: <c>WorkPlan_T</c>.</para>
/// </summary>
public class WorkPlan : FullAuditedTenantEntity, IActivatable, ICompanyScoped
{
    public int CompanyId { get; set; }

    public DateTime StartDate { get; set; }

    public string? RevisionNo { get; set; }

    public DateTime RevisionDate { get; set; }

    public string? DocumentNo { get; set; }

    public DateTime PublicationDate { get; set; }

    /// <summary>The OHS specialist who prepared the plan. (Legacy: <c>UzmanId</c>)</summary>
    public int? SpecialistUserId { get; set; }

    /// <summary>The workplace physician who prepared the plan. (Legacy: <c>DoktorId</c>)</summary>
    public int? PhysicianUserId { get; set; }

    public int? ApproverUserId { get; set; }

    /// <summary>The checklist definition the plan is bound to. (Legacy: <c>CheckListId</c>)</summary>
    public int? ControlItemListId { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Whether the plan has been transferred to the integrated external system. (Legacy: <c>Aktarildi</c> bool?)</summary>
    public bool Transferred { get; set; }

    /// <summary>
    /// Reference to the previous year's plan revision (self-referencing FK). (Legacy: <c>CPId_E</c>)
    /// </summary>
    public int? PreviousPlanId { get; set; }
}
