using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Ibys;
using Ensa.Domain.Membership;
using Ensa.Domain.Health;
using Ensa.Domain.Health.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Health;

/// <summary>
/// EF Core implementation of <see cref="IMedicalExaminationFormRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class MedicalExaminationFormRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<MedicalExaminationForm>(context, dataFilter), IMedicalExaminationFormRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// <b>N+1 PREVENTION:</b> each of the 6 normalised child tables is fetched with a <b>single</b>
    /// query keyed on the form id; no query is issued per row. The total query count is at most 12
    /// regardless of the form content (form + employee + company + physician + 6 child tables +
    /// previous examination + IBYS query number).
    /// </remarks>
    public async Task<MedicalExaminationFormNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var form = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (form is null)
        {
            return null;
        }

        var navigation = new MedicalExaminationFormNavigation { Form = form };

        navigation.Employee = await Context.Set<CompanyEmployee>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == form.CompanyEmployeeId, cancellationToken);

        if (form.CompanyId is { } companyId)
        {
            navigation.Company = await Context.Set<Company>()
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == companyId, cancellationToken);
        }

        if (form.PhysicianUserId is { } physicianId)
        {
            // Only the first and last name are needed; the whole User row is not loaded into memory.
            navigation.PhysicianFullName = await Context.Set<UserProfile>()
                .AsNoTracking()
                .Where(k => k.UserId == physicianId)
                .Select(k => k.Name + " " + k.LastName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // ---------------- Normalised child lists (one SINGLE query each) ----------------
        navigation.Complaints = await Context.Set<MedicalExamComplaint>()
            .AsNoTracking()
            .Where(x => x.MedicalExaminationFormId == id)
            .OrderBy(x => x.ComplaintType)
            .ToListAsync(cancellationToken);

        navigation.FizikFindings = await Context.Set<MedicalExamPhysicalFinding>()
            .AsNoTracking()
            .Where(x => x.MedicalExaminationFormId == id)
            .OrderBy(x => x.System)
            .ToListAsync(cancellationToken);

        navigation.LabTests = await Context.Set<MedicalExamLabTest>()
            .AsNoTracking()
            .Where(x => x.MedicalExaminationFormId == id)
            .OrderBy(x => x.LabTestType)
            .ToListAsync(cancellationToken);

        navigation.Habits = await Context.Set<MedicalExamHabit>()
            .AsNoTracking()
            .Where(x => x.MedicalExaminationFormId == id)
            .OrderBy(x => x.HabitType)
            .ToListAsync(cancellationToken);

        navigation.WorkConditions = await Context.Set<MedicalExamWorkCondition>()
            .AsNoTracking()
            .Where(x => x.MedicalExaminationFormId == id)
            .OrderBy(x => x.ConditionType)
            .ToListAsync(cancellationToken);

        navigation.Immunizations = await Context.Set<MedicalExamImmunization>()
            .AsNoTracking()
            .Where(x => x.MedicalExaminationFormId == id)
            .OrderBy(x => x.ImmunizationType)
            .ToListAsync(cancellationToken);

        // ---------------- Derived indicators ----------------
        navigation.PreviousExaminationDate = await GetReadOnlyQueryable()
            .Where(f => f.CompanyEmployeeId == form.CompanyEmployeeId
                        && f.Id != form.Id
                        && f.ExaminationDate <= form.ExaminationDate)
            .OrderByDescending(f => f.ExaminationDate)
            .Select(f => (DateTime?)f.ExaminationDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (form.IbysQueryId is { } queryId)
        {
            navigation.IbysQueryNo = await Context.Set<IbysQuery>()
                .AsNoTracking()
                .Where(s => s.Id == queryId)
                .Select(s => s.QueryNo)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return navigation;
    }

    /// <inheritdoc />
    public Task<MedicalExaminationForm?> GetLatestExaminationAsync(
        int companyEmployeeId,
        MedicalReportType? reportType = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetReadOnlyQueryable().Where(f => f.CompanyEmployeeId == companyEmployeeId);

        if (reportType is { } type)
        {
            query = query.Where(f => f.ReportType == type);
        }

        return query
            .OrderByDescending(f => f.ExaminationDate)
            .ThenByDescending(f => f.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<MedicalExaminationForm>> GetDurationExpiredAsync(
        int companyId,
        DateTime referenceDate,
        int maxResultCount = 100,
        CancellationToken cancellationToken = default)
    {
        var takeCount = Math.Clamp(maxResultCount, 1, 1000);
        var reference = referenceDate.Date;

        return GetReadOnlyQueryable()
            .Where(f => f.CompanyId == companyId
                        && f.ValidityDate != null
                        && f.ValidityDate <= reference)
            .OrderBy(f => f.ValidityDate)
            .Take(takeCount)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// This is a bulk submission queue: the oldest record is processed first and the result set is capped
    /// in the database with <c>Take</c> — the whole table is never loaded into memory.
    /// </remarks>
    public Task<List<MedicalExaminationForm>> GetByIbysStatusAsync(
        IbysSubmissionStatus status,
        int maxResultCount = 100,
        CancellationToken cancellationToken = default)
    {
        var takeCount = Math.Clamp(maxResultCount, 1, 1000);

        return GetReadOnlyQueryable()
            .Where(f => f.IbysStatus == status)
            .OrderBy(f => f.ExaminationDate)
            .ThenBy(f => f.Id)
            .Take(takeCount)
            .ToListAsync(cancellationToken);
    }
}
