using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Common;

/// <summary>
/// A plan line that goes through the Draft → SubmittedForApproval → Approved/Rejected workflow.
/// <para>
/// Work plan lines and training plan lines run the exact same state machine. It used to be
/// written out twice — once in <c>WorkPlanManager</c>, once inline in the training plan
/// application service — and the two copies had already drifted: resubmitting a work plan line
/// cleared its stale <c>ApprovalDate</c>, resubmitting a training plan line did not. This
/// interface exists so <c>IPlanApprovalManager</c> can own the transition for both.
/// </para>
/// </summary>
public interface IApprovablePlanLine
{
    ApprovalStatus? ApprovalStatus { get; set; }

    int? ForApprovalSenderUserId { get; set; }

    DateTime? ForApprovalSendingDate { get; set; }

    int? ApproverUserId { get; set; }

    DateTime? ApprovalDate { get; set; }

    string? RejectionReason { get; set; }
}
