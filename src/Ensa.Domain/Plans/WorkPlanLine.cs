using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Plans;

/// <summary>
/// A single line of a work plan — one activity scheduled for a specific month at a company.
/// <para>Legacy equivalent: <c>WorkPlanLines_T</c>.</para>
/// <para>
/// NORMALIZATION: the legacy <c>MonthYazi</c> column (the month name as free text) was removed;
/// the presentation layer derives it from <see cref="Month"/>.
/// </para>
/// </summary>
public class WorkPlanLine : FullAuditedTenantEntity, IActivatable, IApprovablePlanLine, ICompanyScoped
{
    public int WorkPlanId { get; set; }

    public int ActivityId { get; set; }

    /// <summary>(<c>Ensa.Domain.Lookups.Period</c>'a FK.)</summary>
    public int? PeriodId { get; set; }

    public int Year { get; set; }

    public int? Month { get; set; }

    /// <summary>(Legacy: <c>Durum</c> int?)</summary>
    public PlanLineStatus? Status { get; set; }

    public DateTime? PerformedDate { get; set; }

    public string? Description { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The matching line in the previous plan revision, so that records carried over from year to
    /// year can be traced. (Legacy: <c>CPId_E</c>)
    /// </summary>
    public int? PreviousLineId { get; set; }

    /// <summary>Approval workflow status of the line. (Legacy: <c>OnayDurumu</c> int?)</summary>
    public ApprovalStatus? ApprovalStatus { get; set; }

    /// <summary>
    /// Why the line was rejected. Kept in its own column so a rejection never rewrites the
    /// author's <c>Description</c>; a re-rejection replaces it instead of appending again.
    /// Cleared whenever the line leaves the rejected state.
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>(Legacy: <c>OnayaGonderenId</c>)</summary>
    public int? ForApprovalSenderUserId { get; set; }

    /// <summary>(Legacy: <c>OnaylayanId</c>)</summary>
    public int? ApproverUserId { get; set; }

    public DateTime? ForApprovalSendingDate { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public int CompanyId { get; set; }

    /// <summary>National identity number of an external trainer who is not registered in the system.</summary>
    public string? InstructorNationalId { get; set; }

    /// <summary>Reference to the trainer when they are a registered user. (NEW field)</summary>
    public int? InstructorUserId { get; set; }

    /// <summary>Evidence document for the activity — FK to the central <c>Document</c> table.</summary>
    public int? DocumentId { get; set; }
}
