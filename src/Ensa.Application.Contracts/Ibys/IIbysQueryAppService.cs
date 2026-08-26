using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Ibys.Dtos;
using Ensa.Application.Contracts.Ibys.Dtos.Navigations;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Ibys;

/// <summary>
/// IBYS (İSG Bilgi Yönetim Sistemi) submission tracking application service.
/// <para>
/// <b>SECURITY.</b> No method of this service returns <c>IbysQuery.XmlData</c> or
/// <c>IbysQuery.SignedData</c>. The XML is an encrypted payload containing clinical data
/// and the signed blob is the CAdES envelope produced with the corporate e-signature;
/// the e-signature licence key is a secret as well. All three stay inside the domain and
/// are read only by the background submission worker.
/// </para>
/// <para>
/// Status transitions are validated by <c>IIbysSubmissionManager.ValidateStatusTransition</c>,
/// which owns the state machine <c>NotSent → Prepared → Sent → Approved | Failed</c>.
/// </para>
/// </summary>
public interface IIbysQueryAppService : IApplicationService
{
    Task<IbysQueryDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Submission with the workplace, the employee and the attached examination forms.</summary>
    Task<IbysQueryNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<IbysQueryListDto>> GetListAsync(
        GetIbysQueryListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submissions of the given type that are still awaiting an IBYS result — the queue the
    /// background status-polling job works through.
    /// </summary>
    Task<ListResultDto<IbysQueryListDto>> GetPendingAsync(
        IbysQueryType type,
        int maxResultCount = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves the submission to <paramref name="status"/> after
    /// <c>IIbysSubmissionManager.ValidateStatusTransition</c> has approved the transition,
    /// recording the service message and the assigned query number.
    /// </summary>
    Task<IbysQueryDto> UpdateStatusAsync(
        int id,
        IbysSubmissionStatus status,
        string? message,
        string? submissionNumber,
        CancellationToken cancellationToken = default);
}
