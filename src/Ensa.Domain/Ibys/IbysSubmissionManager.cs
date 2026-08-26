using Ensa.Domain.Health;
using Ensa.Domain.Services;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Domain.Ibys;

/// <summary>
/// Business rules for IBYS submissions (domain service contract).
/// </summary>
public interface IIbysSubmissionManager : IDomainService
{
    /// <summary>
    /// Reports whether a status transition is allowed (never throws).
    /// </summary>
    bool IsTransitionAllowed(IbysSubmissionStatus current, IbysSubmissionStatus newStatus);

    /// <summary>
    /// Validates a status transition and throws <see cref="BusinessException"/> when it is
    /// invalid. Every code path that changes the status must call this first.
    /// </summary>
    void ValidateStatusTransition(IbysSubmissionStatus current, IbysSubmissionStatus newStatus);

    /// <summary>
    /// Checks whether a medical examination form is ready to be submitted to IBYS.
    /// </summary>
    Task<bool> CanSubmitAsync(
        int medicalExaminationFormId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// <inheritdoc cref="IIbysSubmissionManager"/>
/// <para>
/// <b>STATE MACHINE.</b> The main flow is
/// <c>NotSent → Prepared → Sent → Approved | Failed</c>.
/// The following additional edges are defined:
/// <list type="bullet">
/// <item><c>Prepared → NotSent</c>: the prepared package is cancelled and rebuilt.</item>
/// <item><c>Failed → Prepared</c>: a failed submission is corrected and queued again.</item>
/// <item><c>NotSent | Prepared | Failed → Cancelled</c>: the submission is abandoned.</item>
/// </list>
/// <c>Approved</c> and <c>Cancelled</c> are TERMINAL states; an approved submission is permanent
/// in IBYS and cannot be withdrawn by the application.
/// A transition to the same status (an idempotent repeat) is always considered valid.
/// </para>
/// </summary>
public class IbysSubmissionManager : DomainService, IIbysSubmissionManager
{
    /// <summary>The states reachable from each state.</summary>
    private static readonly IReadOnlyDictionary<IbysSubmissionStatus, IbysSubmissionStatus[]> ValidTransitions =
        new Dictionary<IbysSubmissionStatus, IbysSubmissionStatus[]>
        {
            [IbysSubmissionStatus.NotSent] =
            [
                IbysSubmissionStatus.Prepared,
                IbysSubmissionStatus.Cancelled
            ],
            [IbysSubmissionStatus.Prepared] =
            [
                IbysSubmissionStatus.Sent,
                IbysSubmissionStatus.NotSent,
                IbysSubmissionStatus.Cancelled
            ],
            [IbysSubmissionStatus.Sent] =
            [
                IbysSubmissionStatus.Approved,
                IbysSubmissionStatus.Failed
            ],
            [IbysSubmissionStatus.Failed] =
            [
                IbysSubmissionStatus.Prepared,
                IbysSubmissionStatus.Cancelled
            ],
            [IbysSubmissionStatus.Approved] = [],
            [IbysSubmissionStatus.Cancelled] = []
        };

    private readonly IMedicalExaminationFormRepository _examinationFormRepository;

    public IbysSubmissionManager(IMedicalExaminationFormRepository examinationFormRepository)
        => _examinationFormRepository = examinationFormRepository;

    /// <inheritdoc />
    public bool IsTransitionAllowed(IbysSubmissionStatus current, IbysSubmissionStatus newStatus)
    {
        if (current == newStatus)
        {
            return true;
        }

        return ValidTransitions.TryGetValue(current, out var targets)
               && Array.IndexOf(targets, newStatus) >= 0;
    }

    /// <inheritdoc />
    public void ValidateStatusTransition(IbysSubmissionStatus current, IbysSubmissionStatus newStatus)
    {
        if (IsTransitionAllowed(current, newStatus))
        {
            return;
        }

        throw new BusinessException(
            $"An IBYS submission cannot move from '{current}' to '{newStatus}'.",
            "Ensa:Ibys:InvalidStatusTransition",
            $"Current: {current}, Target: {newStatus}");
    }

    /// <inheritdoc />
    public async Task<bool> CanSubmitAsync(
        int medicalExaminationFormId,
        CancellationToken cancellationToken = default)
    {
        var form = await _examinationFormRepository.FindAsync(medicalExaminationFormId, cancellationToken);

        if (form is null)
        {
            throw new EntityNotFoundException(typeof(MedicalExaminationForm), medicalExaminationFormId);
        }

        // A submission that has reached a terminal state cannot be sent again.
        if (form.IbysStatus is IbysSubmissionStatus.Approved or IbysSubmissionStatus.Cancelled)
        {
            return false;
        }

        // Already sent; the result is still pending.
        if (form.IbysStatus == IbysSubmissionStatus.Sent)
        {
            return false;
        }

        // The fields the IBYS XML requires must all be populated.
        return form.CompanyId.HasValue
               && form.ReportType != MedicalReportType.Unspecified
               && form.Opinion != FitnessForWorkOpinion.Unspecified
               && form.ExaminationDate != default
               && !string.IsNullOrWhiteSpace(form.IbysOccupationCode);
    }
}
