using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Health;
using Ensa.Domain.Health.Navigations;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Health;

/// <summary>
/// EF Core implementation of <see cref="IEPrescriptionRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class EPrescriptionRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<EPrescription>(context, dataFilter), IEPrescriptionRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// <b>N+1 PREVENTION:</b> the SKRS/ICD-10 counterparts of the medication and diagnosis lines are
    /// fetched in bulk with one <c>Contains</c> query each rather than per line, and matched up in memory.
    /// The total query count is at most 9 regardless of the number of lines.
    /// </remarks>
    public async Task<EPrescriptionNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var prescription = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (prescription is null)
        {
            return null;
        }

        var navigation = new EPrescriptionNavigation { EPrescription = prescription };

        if (prescription.PatientCompanyEmployeeId is { } employeeId)
        {
            navigation.Patient = await Context.Set<CompanyEmployee>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == employeeId, cancellationToken);
        }

        // ---------------- Medication lines ----------------
        var medicationLines = await Context.Set<EPrescriptionMedication>()
            .AsNoTracking()
            .Where(x => x.EPrescriptionId == id)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var medicationIds = medicationLines.ConvertAll(x => x.MedicationId).Distinct().ToList();
        var routeIds = medicationLines.ConvertAll(x => x.UsageMethodId).Distinct().ToList();
        var doseIds = medicationLines.ConvertAll(x => x.UsageDoseUnitId).Distinct().ToList();
        var periodIds = medicationLines.ConvertAll(x => x.UsagePeriodUnitId).Distinct().ToList();

        List<LookupLine> medications = medicationIds.Count == 0
            ? []
            : await Context.Set<Medication>()
                .AsNoTracking()
                .Where(x => medicationIds.Contains(x.Id))
                .Select(x => new LookupLine(x.Id, x.MedicationName))
                .ToListAsync(cancellationToken);

        List<LookupLine> routes = routeIds.Count == 0
            ? []
            : await Context.Set<MedicationRoute>()
                .AsNoTracking()
                .Where(x => routeIds.Contains(x.Id))
                .Select(x => new LookupLine(x.Id, x.Name))
                .ToListAsync(cancellationToken);

        List<LookupLine> doseUnits = doseIds.Count == 0
            ? []
            : await Context.Set<MedicationDoseUnit>()
                .AsNoTracking()
                .Where(x => doseIds.Contains(x.Id))
                .Select(x => new LookupLine(x.Id, x.Name))
                .ToListAsync(cancellationToken);

        List<LookupLine> periodUnits = periodIds.Count == 0
            ? []
            : await Context.Set<MedicationFrequencyUnit>()
                .AsNoTracking()
                .Where(x => periodIds.Contains(x.Id))
                .Select(x => new LookupLine(x.Id, x.Name))
                .ToListAsync(cancellationToken);

        navigation.Medications = medicationLines.ConvertAll(line => new EPrescriptionMedicationNavigation
        {
            Medication = line,
            MedicationName = medications.Find(x => x.Id == line.MedicationId)?.Name,
            UsageMethodName = routes.Find(x => x.Id == line.UsageMethodId)?.Name,
            DoseUnitName = doseUnits.Find(x => x.Id == line.UsageDoseUnitId)?.Name,
            PeriodUnitName = periodUnits.Find(x => x.Id == line.UsagePeriodUnitId)?.Name
        });

        // ---------------- Diagnosis lines ----------------
        var diagnosisLines = await Context.Set<EPrescriptionDiagnosis>()
            .AsNoTracking()
            .Where(x => x.EPrescriptionId == id)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        // Diagnoses can be linked either by id or by code; both are resolved in a SINGLE query.
        var icd10Ids = diagnosisLines
            .Where(x => x.Icd10Id.HasValue)
            .Select(x => x.Icd10Id!.Value)
            .Distinct()
            .ToList();

        var icd10Codes = diagnosisLines
            .Where(x => !x.Icd10Id.HasValue && !string.IsNullOrWhiteSpace(x.Icd10Code))
            .Select(x => x.Icd10Code)
            .Distinct()
            .ToList();

        List<Icd10Line> icd10lar = icd10Ids.Count == 0 && icd10Codes.Count == 0
            ? []
            : await Context.Set<Icd10>()
                .AsNoTracking()
                .Where(x => icd10Ids.Contains(x.Id) || icd10Codes.Contains(x.Code))
                .Select(x => new Icd10Line(x.Id, x.Code, x.Name))
                .ToListAsync(cancellationToken);

        navigation.Diagnoses = diagnosisLines.ConvertAll(line => new EPrescriptionDiagnosisNavigation
        {
            Diagnosis = line,
            Icd10Name = line.Icd10Id is { } icd10Id
                ? icd10lar.Find(x => x.Id == icd10Id)?.Name
                : icd10lar.Find(x => x.Code == line.Icd10Code)?.Name
        });

        return navigation;
    }

    /// <inheritdoc />
    public Task<EPrescription?> FindByEPrescriptionCodeAsync(
        string ePrescriptionCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ePrescriptionCode))
        {
            return Task.FromResult<EPrescription?>(null);
        }

        var code = ePrescriptionCode.Trim();

        return GetReadOnlyQueryable()
            .FirstOrDefaultAsync(r => r.EPrescriptionCode == code, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Because the national id column is encrypted deterministically, the equality comparison can be
    /// translated to SQL and the index can be used.
    /// </remarks>
    public Task<List<EPrescription>> GetPatientHistoryAsync(
        string? patientNationalId = null,
        int? companyEmployeeId = null,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default)
    {
        var takeCount = Math.Clamp(maxResultCount, 1, 500);

        var query = GetReadOnlyQueryable();

        if (companyEmployeeId is { } employeeId)
        {
            query = query.Where(r => r.PatientCompanyEmployeeId == employeeId);
        }
        else if (!string.IsNullOrWhiteSpace(patientNationalId))
        {
            var tckn = patientNationalId.Trim();
            query = query.Where(r => r.PatientNationalId == tckn);
        }
        else
        {
            // When no patient is given we return an empty result instead of every prescription;
            // silently listing the whole table would be an unintended data leak.
            return Task.FromResult(new List<EPrescription>());
        }

        return query
            .OrderByDescending(r => r.CreationTime)
            .ThenByDescending(r => r.Id)
            .Take(takeCount)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="EPrescription"/> has no separate physician foreign key; the prescribing physician is
    /// represented by the <c>CreatorId</c> audit field. The count is computed <b>in the database</b>.
    /// </remarks>
    public Task<int> GetIssuedPrescriptionCountAsync(
        int physicianUserId,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {
        var lowerBound = start.Date;
        var upperBound = end.Date.AddDays(1);

        return GetReadOnlyQueryable()
            .CountAsync(
                r => r.CreatorId == physicianUserId
                     && !r.Cancelled
                     && r.CreationTime >= lowerBound
                     && r.CreationTime < upperBound,
                cancellationToken);
    }

    /// <summary>Lightweight lookup projection carrying an id and a name (to avoid loading the whole entity).</summary>
    private sealed record LookupLine(int Id, string Name);

    /// <summary>ICD-10 lookup projection (so that matching can be done by id or by code).</summary>
    private sealed record Icd10Line(int Id, string Code, string Name);
}
