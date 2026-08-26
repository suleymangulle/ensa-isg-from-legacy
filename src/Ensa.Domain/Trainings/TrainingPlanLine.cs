using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Trainings;

/// <summary>
/// A single line of a training plan — one training scheduled for a specific month at a company.
/// <para>Legacy equivalent: <c>TrainingPlanLines_T</c>.</para>
/// <para>
/// NORMALIZATION: the legacy <c>MonthYazi</c> column (the month name as free text) was removed;
/// the presentation layer derives it from <see cref="Month"/>. The free-text legacy
/// <c>IBYSQueryNo</c> was normalized into the <see cref="IbysQueryId"/> FK.
/// <see cref="IbysStatusCode"/> and <see cref="IbysMessage"/> — the external integration messages —
/// are kept for backward compatibility.
/// </para>
/// <para>
/// The free-text legacy <c>InstructorNationalId</c>/<c>InstructorTitle</c>/<c>InstructorFullName</c>
/// columns are kept for the external trainer case, where the trainer is not a registered user. The
/// <see cref="InstructorUserId"/> FK was added so that a trainer who is a system user can be linked
/// directly.
/// </para>
/// </summary>
public class TrainingPlanLine : FullAuditedTenantEntity, IActivatable, IApprovablePlanLine, ICompanyScoped
{
    public int TrainingPlanId { get; set; }

    public int TrainingId { get; set; }

    /// <summary>(Legacy: <c>FirmaId</c> — held on the line as well; it is the same company as on the plan header.)</summary>
    public int? CompanyId { get; set; }

    /// <summary>Planned training duration in minutes. (Legacy: <c>Sure</c>)</summary>
    public int DurationMinutes { get; set; }

    public int? Year { get; set; }

    public int? Month { get; set; }

    /// <summary>(Legacy: <c>Durum</c> int)</summary>
    public PlanLineStatus Status { get; set; } = PlanLineStatus.Planned;

    /// <summary>Approval workflow status of the line. (Legacy: <c>OnayDurumu</c> int?)</summary>
    /// <summary>
    /// Why the line was rejected. Kept in its own column so a rejection never rewrites the
    /// author's <c>Description</c>; a re-rejection replaces it instead of appending again.
    /// Cleared whenever the line leaves the rejected state.
    /// </summary>
    public string? RejectionReason { get; set; }

    public ApprovalStatus? ApprovalStatus { get; set; }

    public DateTime? PerformedDate { get; set; }

    /// <summary>Free text naming the process that produced the line, e.g. "Sistem"/"Manuel". (Legacy: <c>Kaynak</c>)</summary>
    public string? Source { get; set; }

    public string? Description { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The matching line in the previous plan revision, so that records carried over from year to
    /// year can be traced. (Legacy: <c>EPId_E</c>)
    /// </summary>
    public int? PreviousLineId { get; set; }

    /// <summary>(Legacy: <c>OnayaGonderenId</c>)</summary>
    public int? ForApprovalSenderUserId { get; set; }

    /// <summary>(Legacy: <c>OnaylayanId</c>)</summary>
    public int? ApproverUserId { get; set; }

    public DateTime? ForApprovalSendingDate { get; set; }

    public DateTime? ApprovalDate { get; set; }

    /// <summary>National identity number of an external trainer who is not registered in the system.</summary>
    public string? InstructorNationalId { get; set; }

    public string? InstructorTitle { get; set; }

    public string? InstructorFullName { get; set; }

    /// <summary>Reference to the trainer when they are a registered user. (NEW field)</summary>
    public int? InstructorUserId { get; set; }

    /// <summary>(Legacy: <c>EgitimYeri</c> int?)</summary>
    public TrainingLocation? TrainingLocation { get; set; }

    /// <summary>(Legacy: <c>EgitimTuru</c> int?)</summary>
    public TrainingType? TrainingType { get; set; }

    /// <summary>Submission status towards the external system (İBYS). (Legacy: <c>IBYSDurum</c> string "-1"/"0"/"1")</summary>
    public IbysSubmissionStatus IbysStatus { get; set; } = IbysSubmissionStatus.NotSent;

    /// <summary>
    /// FK to the İBYS query record. (Legacy: <c>IBYSSorguNo</c> was a free-text query number; this is
    /// the normalized reference.)
    /// </summary>
    public int? IbysQueryId { get; set; }

    /// <summary>Raw status code returned by the external system. (Legacy: <c>IbysDurumKodu</c>)</summary>
    public string? IbysStatusCode { get; set; }

    /// <summary>Description or error message returned by the external system. (Legacy: <c>IbysMessage</c>)</summary>
    public string? IbysMessage { get; set; }

    /// <summary>Evidence document for the training — FK to the central <c>Document</c> table.</summary>
    public int? DocumentId { get; set; }
}
