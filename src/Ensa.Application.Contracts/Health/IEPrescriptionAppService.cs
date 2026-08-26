using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Health.Dtos;
using Ensa.Application.Contracts.Health.Dtos.Navigations;

namespace Ensa.Application.Contracts.Health;

/// <summary>
/// E-prescription application service (MEDULA / e-Prescription records written by the
/// occupational physician).
/// <para>
/// <b>PRIVACY.</b> Prescribed medication and ICD-10 diagnoses are health data. They are
/// returned only by <see cref="GetWithNavigationAsync"/> for one record at a time; the
/// list projection carries the prescription envelope only.
/// </para>
/// </summary>
public interface IEPrescriptionAppService : IApplicationService
{
    Task<EPrescriptionDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prescription with the patient, the medication lines (SKRS names resolved) and the
    /// diagnosis lines (ICD-10 names resolved).
    /// </summary>
    Task<EPrescriptionNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<EPrescriptionListDto>> GetListAsync(
        GetEPrescriptionListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Creates the prescription together with its medication and diagnosis lines.</summary>
    Task<EPrescriptionDto> CreateAsync(
        CreateEPrescriptionDto input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the prescription header and replaces both line sets.
    /// A prescription that has already been cancelled can no longer be updated.
    /// </summary>
    Task<EPrescriptionDto> UpdateAsync(
        int id,
        UpdateEPrescriptionDto input,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the prescription and records the reason. Cancelling a prescription that is
    /// already cancelled is rejected.
    /// </summary>
    Task<EPrescriptionDto> CancelAsync(
        int id,
        string reason,
        CancellationToken cancellationToken = default);
}
