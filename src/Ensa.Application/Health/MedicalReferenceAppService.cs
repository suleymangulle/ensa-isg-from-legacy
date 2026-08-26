using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Health;
using Ensa.Application.Contracts.Health.Dtos;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Health;
using Ensa.Domain.Repositories;

namespace Ensa.Application.Health;

/// <summary>
/// Read-only search over the host SKRS reference catalogues.
/// <para>
/// Every method is guarded by <c>Ensa.MedicalExamination</c> — the permission a clinician
/// already holds. The catalogues are host data with no tenant column and no personal data,
/// so no tenant predicate is applied here; the global query filter treats
/// <c>TenantId == null</c> rows as visible to everyone.
/// </para>
/// <para>
/// The matching logic, including the Turkish-character normalisation carried over from the
/// legacy system, lives in <see cref="IIcd10Repository"/> and <see cref="IMedicationRepository"/>.
/// This service only clamps the result count and maps.
/// </para>
/// </summary>
public class MedicalReferenceAppService(
    IServiceProvider serviceProvider,
    IIcd10Repository icd10Repository,
    IMedicationRepository medicationRepository,
    IReadOnlyRepository<MedicationRoute> medicationRouteRepository,
    IReadOnlyRepository<MedicationDoseUnit> medicationDoseUnitRepository,
    IReadOnlyRepository<MedicationFrequencyUnit> medicationFrequencyUnitRepository)
    : EnsaAppService(serviceProvider), IMedicalReferenceAppService
{
    /// <summary>Default number of search hits, matching the legacy behaviour.</summary>
    private const int DefaultMaxResultCount = 25;

    /// <summary>Hard ceiling so that a search cannot be turned into a bulk catalogue export.</summary>
    private const int AbsoluteMaxResultCount = 200;

    /// <summary>Shortest filter accepted; anything shorter would return most of the table.</summary>
    private const int MinimumFilterLength = 2;

    /// <inheritdoc />
    public async Task<ListResultDto<Icd10LookupDto>> SearchIcd10Async(
        string filter,
        int maxResultCount = DefaultMaxResultCount,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Default);

        var search = filter?.Trim();

        if (string.IsNullOrEmpty(search) || search.Length < MinimumFilterLength)
        {
            return new ListResultDto<Icd10LookupDto>([]);
        }

        var records = await icd10Repository.SearchAsync(
            search,
            NormalizeMaxResultCount(maxResultCount),
            cancellationToken);

        return new ListResultDto<Icd10LookupDto>(
            ObjectMapper.Map<List<Icd10>, List<Icd10LookupDto>>(records));
    }

    /// <inheritdoc />
    public async Task<ListResultDto<MedicationLookupDto>> SearchMedicationsAsync(
        string filter,
        int maxResultCount = DefaultMaxResultCount,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Default);

        var search = filter?.Trim();

        if (string.IsNullOrEmpty(search) || search.Length < MinimumFilterLength)
        {
            return new ListResultDto<MedicationLookupDto>([]);
        }

        var records = await medicationRepository.SearchByBarcodeOrNameAsync(
            search,
            NormalizeMaxResultCount(maxResultCount),
            cancellationToken);

        return new ListResultDto<MedicationLookupDto>(
            ObjectMapper.Map<List<Medication>, List<MedicationLookupDto>>(records));
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetMedicationRoutesAsync(
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Default);

        var records = await medicationRouteRepository.GetListAsync(r => r.IsActive, cancellationToken);

        return ToLookupResult(records);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetMedicationDoseUnitsAsync(
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Default);

        var records = await medicationDoseUnitRepository.GetListAsync(u => u.IsActive, cancellationToken);

        return ToLookupResult(records);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetMedicationFrequencyUnitsAsync(
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Default);

        var records = await medicationFrequencyUnitRepository.GetListAsync(u => u.IsActive, cancellationToken);

        return ToLookupResult(records);
    }

    // -----------------------------------------------------------------

    private static int NormalizeMaxResultCount(int requested)
        => requested <= 0
            ? DefaultMaxResultCount
            : Math.Min(requested, AbsoluteMaxResultCount);

    /// <summary>Projects any SKRS code list to the shared lookup shape, ordered by name.</summary>
    private static ListResultDto<LookupDto> ToLookupResult<TEntity>(List<TEntity> records)
        where TEntity : SkrsReferenceEntity
    {
        var items = records
            .OrderBy(r => r.Name, StringComparer.CurrentCulture)
            .Select(r => new LookupDto
            {
                Id = r.Id,
                DisplayName = r.Name,
                Code = r.Code?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                IsActive = r.IsActive
            })
            .ToList();

        return new ListResultDto<LookupDto>(items);
    }
}
