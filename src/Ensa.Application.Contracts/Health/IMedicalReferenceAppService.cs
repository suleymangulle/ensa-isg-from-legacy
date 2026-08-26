using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Health.Dtos;

namespace Ensa.Application.Contracts.Health;

/// <summary>
/// Read-only search over the host SKRS reference catalogues used while writing an
/// e-prescription or a medical examination form.
/// <para>
/// These tables are host data (no tenant, no personal data) seeded by <c>DbMigrator</c>.
/// The service exposes no write operation and is guarded by
/// <c>Ensa.MedicalExamination</c> — the permission a clinician already holds.
/// </para>
/// <para>
/// The matching itself, including the Turkish-character normalisation inherited from the
/// legacy system, lives in <c>IIcd10Repository</c> and <c>IMedicationRepository</c>;
/// this service only orchestrates and maps.
/// </para>
/// </summary>
public interface IMedicalReferenceAppService : IApplicationService
{
    /// <summary>Searches ICD-10 diagnoses by name fragment or code.</summary>
    Task<ListResultDto<Icd10LookupDto>> SearchIcd10Async(
        string filter,
        int maxResultCount = 25,
        CancellationToken cancellationToken = default);

    /// <summary>Searches active medications by exact barcode or name fragment.</summary>
    Task<ListResultDto<MedicationLookupDto>> SearchMedicationsAsync(
        string filter,
        int maxResultCount = 25,
        CancellationToken cancellationToken = default);

    /// <summary>Routes of administration (oral, intramuscular, intravenous ...).</summary>
    Task<ListResultDto<LookupDto>> GetMedicationRoutesAsync(CancellationToken cancellationToken = default);

    /// <summary>Dose units (tablet, measure, ml ...).</summary>
    Task<ListResultDto<LookupDto>> GetMedicationDoseUnitsAsync(CancellationToken cancellationToken = default);

    /// <summary>Frequency units (hour, day, week ...).</summary>
    Task<ListResultDto<LookupDto>> GetMedicationFrequencyUnitsAsync(CancellationToken cancellationToken = default);
}
