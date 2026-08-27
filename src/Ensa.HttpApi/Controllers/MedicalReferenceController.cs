using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Health;
using Ensa.Application.Contracts.Health.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Read-only SKRS reference catalogue endpoints — <c>api/medical-reference</c>.
/// <para>
/// Host data with no tenant column and no personal data, seeded by <c>DbMigrator</c>. Every
/// endpoint is a search or a code list; none of them writes.
/// </para>
/// </summary>
public class MedicalReferenceController(IMedicalReferenceAppService appService) : EnsaController
{
    /// <summary>Searches ICD-10 diagnoses by name fragment or code.</summary>
    [HttpGet("icd10")]
    [ProducesResponseType<ListResultDto<Icd10LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<Icd10LookupDto>> SearchIcd10Async(
        [FromQuery] string filter,
        [FromQuery] int maxResultCount,
        CancellationToken cancellationToken)
        => appService.SearchIcd10Async(filter, maxResultCount, cancellationToken);

    /// <summary>Searches active medications by exact barcode or name fragment.</summary>
    [HttpGet("medications")]
    [ProducesResponseType<ListResultDto<MedicationLookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<MedicationLookupDto>> SearchMedicationsAsync(
        [FromQuery] string filter,
        [FromQuery] int maxResultCount,
        CancellationToken cancellationToken)
        => appService.SearchMedicationsAsync(filter, maxResultCount, cancellationToken);

    /// <summary>Routes of administration.</summary>
    [HttpGet("medication-routes")]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetMedicationRoutesAsync(CancellationToken cancellationToken)
        => appService.GetMedicationRoutesAsync(cancellationToken);

    /// <summary>Dose units.</summary>
    [HttpGet("medication-dose-units")]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetMedicationDoseUnitsAsync(CancellationToken cancellationToken)
        => appService.GetMedicationDoseUnitsAsync(cancellationToken);

    /// <summary>Frequency units.</summary>
    [HttpGet("medication-frequency-units")]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetMedicationFrequencyUnitsAsync(CancellationToken cancellationToken)
        => appService.GetMedicationFrequencyUnitsAsync(cancellationToken);
}
