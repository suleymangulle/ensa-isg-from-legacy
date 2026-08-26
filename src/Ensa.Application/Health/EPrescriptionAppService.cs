using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Health;
using Ensa.Application.Contracts.Health.Dtos;
using Ensa.Application.Contracts.Health.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Companies;
using Ensa.Domain.Health;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Health;

/// <summary>
/// E-prescription application service.
/// <para>
/// <b>PRIVACY.</b> Medication and ICD-10 diagnosis lines are health data. They are returned
/// only by <see cref="GetWithNavigationAsync"/>, for one explicitly requested prescription;
/// the list projection carries the prescription envelope alone. The patient's national id is
/// stored encrypted in the domain and is never used as a free-text search key here.
/// </para>
/// </summary>
public class EPrescriptionAppService(
    IServiceProvider serviceProvider,
    IEPrescriptionRepository prescriptionRepository,
    IRepository<EPrescriptionMedication> medicationLineRepository,
    IRepository<EPrescriptionDiagnosis> diagnosisLineRepository,
    IReadOnlyRepository<CompanyEmployee> employeeRepository)
    : EnsaAppService(serviceProvider), IEPrescriptionAppService
{
    /// <inheritdoc />
    public async Task<EPrescriptionDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.EPrescription.Default);

        var prescription = await prescriptionRepository.FindAsync(id, cancellationToken)
                           ?? throw new EntityNotFoundException(typeof(EPrescription), id);

        return ObjectMapper.Map<EPrescription, EPrescriptionDto>(prescription);
    }

    /// <inheritdoc />
    public async Task<EPrescriptionNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.EPrescription.Default);

        // A single repository call returns the header, the patient and both line sets with
        // their SKRS names already joined — no per-line lookup happens here.
        var navigation = await prescriptionRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(EPrescription), id);

        Logger.LogInformation(
            "E-prescription detail read. PrescriptionId={PrescriptionId}, UserId={UserId}", id, CurrentUser.Id);

        return new EPrescriptionNavigationDto
        {
            EPrescription = ObjectMapper.Map<EPrescription, EPrescriptionDto>(navigation.EPrescription),
            Patient = navigation.Patient is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Patient.Id,
                    DisplayName = $"{navigation.Patient.Name} {navigation.Patient.LastName}".Trim(),
                    IsActive = navigation.Patient.IsActive
                },
            Medications =
            [
                .. navigation.Medications.Select(m => new EPrescriptionMedicationLineDto
                {
                    Id = m.Medication.Id,
                    MedicationId = m.Medication.MedicationId,
                    MedicationName = m.MedicationName,
                    MedicationBarcode = m.Medication.MedicationBarcode,
                    UsageMethodId = m.Medication.UsageMethodId,
                    UsageMethodName = m.UsageMethodName,
                    UsageDoseUnitId = m.Medication.UsageDoseUnitId,
                    DoseUnitName = m.DoseUnitName,
                    UsagePeriodUnitId = m.Medication.UsagePeriodUnitId,
                    PeriodUnitName = m.PeriodUnitName,
                    Box = m.Medication.Box,
                    Dose = m.Medication.Dose,
                    DoseFraction = m.Medication.DoseFraction,
                    Period = m.Medication.Period,
                    MedicationDescription = m.Medication.MedicationDescription
                })
            ],
            Diagnoses =
            [
                .. navigation.Diagnoses.Select(d => new EPrescriptionDiagnosisLineDto
                {
                    Id = d.Diagnosis.Id,
                    Icd10Code = d.Diagnosis.Icd10Code,
                    Icd10Id = d.Diagnosis.Icd10Id,
                    Icd10Name = d.Icd10Name
                })
            ]
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<EPrescriptionListDto>> GetListAsync(
        GetEPrescriptionListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.EPrescription.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "CreationTime DESC");

        var total = await prescriptionRepository.GetCountAsync(predicate, cancellationToken);

        var records = await prescriptionRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<EPrescription>, List<EPrescriptionListDto>>(records);

        // Patient names resolved with one batched query, not one per row.
        var employeeIds = records
            .Where(p => p.PatientCompanyEmployeeId.HasValue)
            .Select(p => p.PatientCompanyEmployeeId!.Value)
            .Distinct()
            .ToList();

        if (employeeIds.Count > 0)
        {
            var employees = await employeeRepository.GetListAsync(
                e => employeeIds.Contains(e.Id),
                cancellationToken);

            var names = employees.ToDictionary(e => e.Id, e => $"{e.Name} {e.LastName}".Trim());

            foreach (var item in items)
            {
                if (item.PatientCompanyEmployeeId is { } employeeId && names.TryGetValue(employeeId, out var name))
                {
                    item.PatientFullName = name;
                }
            }
        }

        return new PagedResultDto<EPrescriptionListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<EPrescriptionDto> CreateAsync(
        CreateEPrescriptionDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.EPrescription.Create);

        ValidateLines(input);

        var prescription = ObjectMapper.Map<CreateEPrescriptionDto, EPrescription>(input);

        // No prescription manager exists; this service owns persistence and saves once.
        prescription = await prescriptionRepository.InsertAsync(prescription, autoSave: true, cancellationToken);

        await ReplaceLinesAsync(prescription.Id, input, cancellationToken);

        Logger.LogInformation("E-prescription created. PrescriptionId={PrescriptionId}", prescription.Id);

        return ObjectMapper.Map<EPrescription, EPrescriptionDto>(prescription);
    }

    /// <inheritdoc />
    public async Task<EPrescriptionDto> UpdateAsync(
        int id,
        UpdateEPrescriptionDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.EPrescription.Update);

        var prescription = await prescriptionRepository.FindAsync(id, cancellationToken)
                           ?? throw new EntityNotFoundException(typeof(EPrescription), id);

        if (prescription.Cancelled)
        {
            throw new BusinessException(
                "A cancelled prescription can no longer be edited.",
                "Ensa:EPrescription:AlreadyCancelled")
                .WithData("PrescriptionId", id);
        }

        ValidateLines(input);

        ObjectMapper.Map(input, prescription);

        prescription = await prescriptionRepository.UpdateAsync(prescription, autoSave: true, cancellationToken);

        await ReplaceLinesAsync(id, input, cancellationToken);

        return ObjectMapper.Map<EPrescription, EPrescriptionDto>(prescription);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.EPrescription.Delete);

        var prescription = await prescriptionRepository.FindAsync(id, cancellationToken)
                           ?? throw new EntityNotFoundException(typeof(EPrescription), id);

        // A prescription already accepted by the e-prescription service exists outside this
        // system; it is cancelled, never deleted.
        if (!string.IsNullOrWhiteSpace(prescription.EPrescriptionCode))
        {
            throw new BusinessException(
                "A prescription that has been submitted to the e-prescription service cannot be deleted; cancel it instead.",
                "Ensa:EPrescription:SubmittedCannotBeDeleted")
                .WithData("PrescriptionId", id);
        }

        await medicationLineRepository.DeleteDirectAsync(x => x.EPrescriptionId == id, cancellationToken);
        await diagnosisLineRepository.DeleteDirectAsync(x => x.EPrescriptionId == id, cancellationToken);

        await prescriptionRepository.DeleteAsync(prescription, autoSave: true, cancellationToken);

        Logger.LogInformation("E-prescription deleted. PrescriptionId={PrescriptionId}", id);
    }

    /// <inheritdoc />
    public async Task<EPrescriptionDto> CancelAsync(
        int id,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        await CheckPermissionAsync(EnsaPermissions.EPrescription.Update);

        var prescription = await prescriptionRepository.FindAsync(id, cancellationToken)
                           ?? throw new EntityNotFoundException(typeof(EPrescription), id);

        if (prescription.Cancelled)
        {
            throw new BusinessException(
                "This prescription has already been cancelled.",
                "Ensa:EPrescription:AlreadyCancelled")
                .WithData("PrescriptionId", id);
        }

        prescription.Cancelled = true;
        prescription.ResultMessage = reason.Trim();

        prescription = await prescriptionRepository.UpdateAsync(prescription, autoSave: true, cancellationToken);

        Logger.LogInformation("E-prescription cancelled. PrescriptionId={PrescriptionId}", id);

        return ObjectMapper.Map<EPrescription, EPrescriptionDto>(prescription);
    }

    // -----------------------------------------------------------------

    /// <summary>Rejects a prescription that repeats a medication or a diagnosis code.</summary>
    private static void ValidateLines(CreateEPrescriptionDto input)
    {
        var duplicateMedication = input.Medications
            .GroupBy(m => m.MedicationId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateMedication is not null)
        {
            throw new BusinessException(
                "The same medication cannot appear twice on one prescription.",
                "Ensa:EPrescription:DuplicateMedication")
                .WithData("MedicationId", duplicateMedication.Key);
        }

        var duplicateDiagnosis = input.Diagnoses
            .GroupBy(d => d.Icd10Code, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateDiagnosis is not null)
        {
            throw new BusinessException(
                "The same diagnosis code cannot appear twice on one prescription.",
                "Ensa:EPrescription:DuplicateDiagnosis")
                .WithData("Icd10Code", duplicateDiagnosis.Key);
        }
    }

    /// <summary>Replaces both line sets of a prescription in one pass.</summary>
    private async Task ReplaceLinesAsync(
        int prescriptionId,
        CreateEPrescriptionDto input,
        CancellationToken cancellationToken)
    {
        // Physical delete keeps the replacement rows clear of soft-deleted predecessors.
        await medicationLineRepository.DeleteDirectAsync(x => x.EPrescriptionId == prescriptionId, cancellationToken);
        await diagnosisLineRepository.DeleteDirectAsync(x => x.EPrescriptionId == prescriptionId, cancellationToken);

        if (input.Medications.Count > 0)
        {
            var medications = input.Medications
                .Select(m =>
                {
                    var entity = ObjectMapper.Map<SaveEPrescriptionMedicationDto, EPrescriptionMedication>(m);
                    entity.EPrescriptionId = prescriptionId;
                    return entity;
                })
                .ToList();

            await medicationLineRepository.InsertManyAsync(medications, autoSave: true, cancellationToken);
        }

        if (input.Diagnoses.Count > 0)
        {
            var diagnoses = input.Diagnoses
                .Select(d =>
                {
                    var entity = ObjectMapper.Map<SaveEPrescriptionDiagnosisDto, EPrescriptionDiagnosis>(d);
                    entity.EPrescriptionId = prescriptionId;
                    return entity;
                })
                .ToList();

            await diagnosisLineRepository.InsertManyAsync(diagnoses, autoSave: true, cancellationToken);
        }
    }

    private static Expression<Func<EPrescription, bool>>? BuildFilter(GetEPrescriptionListInput input)
    {
        Expression<Func<EPrescription, bool>> predicate = p => true;
        var applied = false;

        if (!string.IsNullOrWhiteSpace(input.PatientNationalId))
        {
            // Exact match only: the column is encrypted, and a partial match on a national
            // id would be an identity-enumeration channel.
            var nationalId = input.PatientNationalId.Trim();
            predicate = Combine(predicate, p => p.PatientNationalId == nationalId);
            applied = true;
        }

        if (input.PatientCompanyEmployeeId is { } employeeId)
        {
            predicate = Combine(predicate, p => p.PatientCompanyEmployeeId == employeeId);
            applied = true;
        }

        if (input.Cancelled is { } cancelled)
        {
            predicate = Combine(predicate, p => p.Cancelled == cancelled);
            applied = true;
        }

        if (input.DateFrom is { } from)
        {
            predicate = Combine(predicate, p => p.CreationTime >= from);
            applied = true;
        }

        if (input.DateTo is { } to)
        {
            predicate = Combine(predicate, p => p.CreationTime <= to);
            applied = true;
        }

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var search = input.Filter.Trim();
            predicate = Combine(predicate, p =>
                (p.EPrescriptionCode != null && p.EPrescriptionCode.Contains(search))
                || (p.ProtocolNo != null && p.ProtocolNo.Contains(search)));
            applied = true;
        }

        return applied ? predicate : null;
    }

    private static Expression<Func<EPrescription, bool>> Combine(
        Expression<Func<EPrescription, bool>> left,
        Expression<Func<EPrescription, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(EPrescription), "p");

        var body = Expression.AndAlso(
            new ParameterRebinder(left.Parameters[0], parameter).Visit(left.Body)!,
            new ParameterRebinder(right.Parameters[0], parameter).Visit(right.Body)!);

        return Expression.Lambda<Func<EPrescription, bool>>(body, parameter);
    }

    /// <summary>Rewrites two separate lambdas onto a single shared parameter.</summary>
    private sealed class ParameterRebinder(ParameterExpression previous, ParameterExpression replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == previous ? replacement : base.VisitParameter(node);
    }
}
