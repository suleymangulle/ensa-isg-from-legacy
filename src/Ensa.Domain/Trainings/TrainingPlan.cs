using Ensa.Domain.Common;

namespace Ensa.Domain.Trainings;

/// <summary>
/// The header — the cover page — of a company's annual training plan.
/// <para>Legacy equivalent: <c>TrainingPlan_T</c>.</para>
/// </summary>
public class TrainingPlan : FullAuditedTenantEntity, IActivatable, ICompanyScoped
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

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Whether the plan has been transferred to the integrated external system, İBYS for instance. (Legacy: <c>Aktarildi</c> bool?)</summary>
    public bool IsTransferred { get; set; }
}
