using Ensa.Domain.Common;
using Ensa.Domain.Plans;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Ensa.Domain.Trainings;

namespace Ensa.Domain.Tests;

/// <summary>
/// The approval workflow used to be written out twice — once in <c>WorkPlanManager</c>, once
/// inline in the training plan application service. These tests pin the single shared
/// implementation, including the field clearing that a hand-written second copy would be
/// likely to forget.
/// </summary>
public class PlanApprovalTests
{
    private static readonly DateTime At = new(2026, 8, 26, 10, 30, 0);

    private readonly PlanApprovalManager _manager = new();

    [Theory]
    [InlineData(null, ApprovalStatus.SubmittedForApproval, true)]
    [InlineData(ApprovalStatus.Draft, ApprovalStatus.SubmittedForApproval, true)]
    [InlineData(ApprovalStatus.SubmittedForApproval, ApprovalStatus.Approved, true)]
    [InlineData(ApprovalStatus.SubmittedForApproval, ApprovalStatus.Rejected, true)]
    [InlineData(ApprovalStatus.Rejected, ApprovalStatus.SubmittedForApproval, true)]
    [InlineData(ApprovalStatus.Draft, ApprovalStatus.Approved, false)]
    [InlineData(ApprovalStatus.Approved, ApprovalStatus.Rejected, false)]
    [InlineData(ApprovalStatus.Approved, ApprovalStatus.SubmittedForApproval, false)]
    public void Allows_only_the_defined_edges(ApprovalStatus? current, ApprovalStatus target, bool expected)
    {
        Assert.Equal(expected, _manager.CanTransition(current, target));
    }

    [Fact]
    public void Rejects_a_disallowed_edge_with_the_callers_error_code()
    {
        var line = new WorkPlanLine { ApprovalStatus = ApprovalStatus.Approved };

        var exception = Assert.Throws<BusinessException>(() => _manager.ApplyTransition(
            line, ApprovalStatus.Rejected, userId: 7, At, "Ensa:WorkPlan:InvalidApprovalTransition"));

        Assert.Equal("Ensa:WorkPlan:InvalidApprovalTransition", exception.Code);
        // The line must be left exactly as it was.
        Assert.Equal(ApprovalStatus.Approved, line.ApprovalStatus);
    }

    [Fact]
    public void Records_who_rejected_the_line_and_why()
    {
        var line = new TrainingPlanLine { ApprovalStatus = ApprovalStatus.SubmittedForApproval };

        _manager.ApplyTransition(
            line, ApprovalStatus.Rejected, userId: 42, At, "Ensa:TrainingPlan:InvalidApprovalTransition",
            reason: "  Missing risk assessment  ");

        Assert.Equal(ApprovalStatus.Rejected, line.ApprovalStatus);
        Assert.Equal(42, line.ApproverUserId);
        Assert.Equal(At, line.ApprovalDate);
        Assert.Equal("Missing risk assessment", line.RejectionReason);
    }

    [Fact]
    public void Resubmission_wipes_the_previous_decision()
    {
        var line = new TrainingPlanLine
        {
            ApprovalStatus = ApprovalStatus.Rejected,
            ApproverUserId = 42,
            ApprovalDate = At.AddDays(-3),
            RejectionReason = "Missing risk assessment"
        };

        _manager.ApplyTransition(
            line, ApprovalStatus.SubmittedForApproval, userId: 9, At, "Ensa:TrainingPlan:InvalidApprovalTransition");

        Assert.Equal(ApprovalStatus.SubmittedForApproval, line.ApprovalStatus);
        Assert.Equal(9, line.ForApprovalSenderUserId);
        Assert.Equal(At, line.ForApprovalSendingDate);
        Assert.Null(line.ApproverUserId);
        Assert.Null(line.ApprovalDate);
        Assert.Null(line.RejectionReason);
    }

    [Fact]
    public void Approval_clears_an_earlier_rejection_reason()
    {
        var line = new WorkPlanLine
        {
            ApprovalStatus = ApprovalStatus.SubmittedForApproval,
            RejectionReason = "Missing risk assessment"
        };

        _manager.ApplyTransition(
            line, ApprovalStatus.Approved, userId: 5, At, "Ensa:WorkPlan:InvalidApprovalTransition");

        Assert.Equal(ApprovalStatus.Approved, line.ApprovalStatus);
        Assert.Equal(5, line.ApproverUserId);
        Assert.Null(line.RejectionReason);
    }

    /// <summary>
    /// Both modules must behave identically; that is the whole reason the workflow was pulled
    /// into one place.
    /// </summary>
    [Fact]
    public void Treats_work_plan_and_training_plan_lines_the_same()
    {
        var workPlanLine = new WorkPlanLine { ApprovalStatus = ApprovalStatus.SubmittedForApproval };
        var trainingPlanLine = new TrainingPlanLine { ApprovalStatus = ApprovalStatus.SubmittedForApproval };

        foreach (IApprovablePlanLine line in new IApprovablePlanLine[] { workPlanLine, trainingPlanLine })
        {
            _manager.ApplyTransition(line, ApprovalStatus.Rejected, userId: 3, At, "Ensa:Test", "same reason");
        }

        Assert.Equal(workPlanLine.ApprovalStatus, trainingPlanLine.ApprovalStatus);
        Assert.Equal(workPlanLine.ApproverUserId, trainingPlanLine.ApproverUserId);
        Assert.Equal(workPlanLine.ApprovalDate, trainingPlanLine.ApprovalDate);
        Assert.Equal(workPlanLine.RejectionReason, trainingPlanLine.RejectionReason);
    }
}
