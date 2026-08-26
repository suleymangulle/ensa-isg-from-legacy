using Ensa.Domain.Common;

namespace Ensa.Domain.Plans;

/// <summary>
/// The work plan template used during the contract stage.
/// <para>Legacy equivalent: <c>AyarlarContract_T</c>.</para>
/// <para>
/// CAUTION: in legacy this table had exactly the same columns as <c>WorkPlan_T</c>; it served as a
/// "draft" work plan template for companies that had not signed a contract yet. The structure was
/// kept as it was; only the name was clarified to <c>ContractTemplate</c>.
/// </para>
/// </summary>
public class ContractTemplate : FullAuditedTenantEntity, IActivatable, ICompanyScoped
{
    public int CompanyId { get; set; }

    public string? RevisionNo { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime RevisionDate { get; set; }

    public string? DocumentNo { get; set; }

    public DateTime PublicationDate { get; set; }

    /// <summary>(Legacy: <c>UzmanId</c>)</summary>
    public int? SpecialistUserId { get; set; }

    /// <summary>(Legacy: <c>DoktorId</c>)</summary>
    public int? PhysicianUserId { get; set; }

    public int? ApproverUserId { get; set; }

    /// <summary>(Legacy: <c>CheckListId</c>)</summary>
    public int? ControlItemListId { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Reference to the real work plan this template becomes once the contract is approved.
    /// (Legacy: <c>CPId_E</c>)
    /// </summary>
    public int? WorkPlanId { get; set; }
}
