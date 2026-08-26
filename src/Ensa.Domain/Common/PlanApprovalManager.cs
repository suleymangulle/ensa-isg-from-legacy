using Ensa.Domain.Services;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Domain.Common;

/// <summary>
/// Owns the approval workflow shared by every plan line (work plans and training plans).
/// <para>
/// Validating the edge and writing the workflow fields is the same rule in both modules, so it
/// is stated once here. The caller supplies its own error code, which keeps the localized
/// message module-specific while the behaviour stays identical.
/// </para>
/// </summary>
public interface IPlanApprovalManager : IDomainService
{
    /// <summary>Whether the line may move from its current status to <paramref name="target"/>.</summary>
    bool CanTransition(ApprovalStatus? current, ApprovalStatus target);

    /// <summary>
    /// Validates the transition and writes the workflow fields on <paramref name="line"/>
    /// <b>in memory</b>. It does not persist — the caller owns the save.
    /// </summary>
    /// <param name="line">The line to move.</param>
    /// <param name="target">Status to move to.</param>
    /// <param name="userId">User performing the transition.</param>
    /// <param name="at">Timestamp to record; the caller's clock, so it stays testable.</param>
    /// <param name="errorCode">Module-specific resource code used when the edge is not allowed.</param>
    /// <param name="reason">Rejection reason; only meaningful when moving to Rejected.</param>
    void ApplyTransition(
        IApprovablePlanLine line,
        ApprovalStatus target,
        int userId,
        DateTime at,
        string errorCode,
        string? reason = null);
}

/// <inheritdoc cref="IPlanApprovalManager"/>
public class PlanApprovalManager : DomainService, IPlanApprovalManager
{
    /// <summary>Allowed edges. Key: current status; value: the statuses reachable from it.</summary>
    private static readonly Dictionary<ApprovalStatus, ApprovalStatus[]> ValidTransitions = new()
    {
        [ApprovalStatus.Draft] = [ApprovalStatus.SubmittedForApproval],
        [ApprovalStatus.SubmittedForApproval] = [ApprovalStatus.Approved, ApprovalStatus.Rejected],
        [ApprovalStatus.Rejected] = [ApprovalStatus.SubmittedForApproval],
        [ApprovalStatus.Approved] = []
    };

    /// <inheritdoc />
    public bool CanTransition(ApprovalStatus? current, ApprovalStatus target)
        => ValidTransitions.TryGetValue(current ?? ApprovalStatus.Draft, out var allowed)
           && allowed.Contains(target);

    /// <inheritdoc />
    public void ApplyTransition(
        IApprovablePlanLine line,
        ApprovalStatus target,
        int userId,
        DateTime at,
        string errorCode,
        string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        var current = line.ApprovalStatus ?? ApprovalStatus.Draft;

        if (!CanTransition(current, target))
        {
            throw new BusinessException(
                    $"A plan line cannot move from '{current}' to '{target}'.",
                    errorCode)
                .WithData("CurrentStatus", current)
                .WithData("TargetStatus", target);
        }

        switch (target)
        {
            case ApprovalStatus.SubmittedForApproval:
                line.ForApprovalSenderUserId = userId;
                line.ForApprovalSendingDate = at;
                // A resubmission wipes the previous decision entirely; leaving the old approval
                // date behind is what made the two hand-written copies disagree.
                line.ApproverUserId = null;
                line.ApprovalDate = null;
                line.RejectionReason = null;
                break;

            case ApprovalStatus.Approved:
                line.ApproverUserId = userId;
                line.ApprovalDate = at;
                line.RejectionReason = null;
                break;

            case ApprovalStatus.Rejected:
                line.ApproverUserId = userId;
                line.ApprovalDate = at;
                line.RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
                break;
        }

        line.ApprovalStatus = target;
    }
}
