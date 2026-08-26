using Ensa.Domain.Common;
using Ensa.Domain.Repositories;
using Ensa.Domain.Risks;
using Ensa.Domain.Services;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Trainings;

namespace Ensa.Domain.Companies;

/// <summary>
/// Computes the outstanding-obligation figures behind <see cref="CompanyComplianceSummary"/>.
/// </summary>
public interface ICompanyComplianceCalculator : IDomainService
{
    /// <summary>
    /// Recomputes one company's summary and stores it. Used on a cache miss, so a company created
    /// a moment ago does not show an empty panel until the next background round.
    /// </summary>
    Task<CompanyComplianceSummary?> RecalculateAsync(int companyId, CancellationToken ct = default);

    /// <summary>
    /// Recomputes every company's summary. Rows whose figures did not move are left untouched, so
    /// <see cref="CompanyComplianceSummary.CalculatedTime"/> keeps meaning "when this last changed".
    /// </summary>
    /// <returns>How many rows were written.</returns>
    Task<int> RecalculateAllAsync(CancellationToken ct = default);
}

/// <summary>
/// The rules behind the compliance panel.
/// <para>
/// <b>Why a domain service.</b> These six counts are a business rule — what "missing training"
/// and "overdue inspection" mean is a statutory question, not a hosting concern. The background
/// job that keeps the table warm lives in the Host; the definition of the numbers lives here, so
/// the job and the cache-miss path can never compute them differently.
/// </para>
/// <para>
/// <b>Reading.</b> The full recalculation deliberately loads each table once and groups in memory
/// rather than issuing six aggregates per company: with a few hundred companies the per-company
/// version is thousands of round trips, and this one is five. The single-company path filters in
/// SQL instead, because there it is one company's rows either way.
/// </para>
/// </summary>
public class CompanyComplianceCalculator(
    IReadOnlyRepository<Company> companyRepository,
    IReadOnlyRepository<CompanyEmployee> employeeRepository,
    IReadOnlyRepository<Equipment> equipmentRepository,
    IReadOnlyRepository<Training> trainingRepository,
    IReadOnlyRepository<EmployeeTrainingProgress> progressRepository,
    IRepository<CompanyComplianceSummary> summaryRepository,
    IClock clock) : DomainService, ICompanyComplianceCalculator
{
    /// <summary>The six counts the compliance panel shows.</summary>
    private readonly record struct Counts(
        int SafetyTrainingNone,
        int SafetyTrainingMissing,
        int HealthTrainingNone,
        int HealthTrainingMissing,
        int PreEmploymentExaminationMissing,
        int EquipmentExaminationMissing);

    /// <inheritdoc />
    public async Task<CompanyComplianceSummary?> RecalculateAsync(
        int companyId,
        CancellationToken ct = default)
    {
        var company = await companyRepository.FindAsync(companyId, ct);
        if (company is null)
        {
            return null;
        }

        var employees = await employeeRepository.GetListAsync(
            employee => employee.CompanyId == companyId && employee.IsActive, ct);

        var equipment = await equipmentRepository.GetListAsync(
            item => item.CompanyId == companyId, ct);

        var healthTrainingIds = await HealthTrainingIdsAsync(ct);

        var employeeIds = employees.ConvertAll(employee => employee.Id);

        var progress = employeeIds.Count == 0
            ? []
            : await progressRepository.GetListAsync(
                row => employeeIds.Contains(row.CompanyEmployeeId) && row.IsActive, ct);

        var progressByEmployee = Group(progress);

        var counts = Calculate(employees, equipment, progressByEmployee, healthTrainingIds, Today);

        var summary = await FindSummaryAsync(companyId, ct);

        if (summary is null)
        {
            summary = new CompanyComplianceSummary
            {
                CompanyId = companyId,
                TenantId = company.TenantId
            };

            Apply(summary, counts);
            return await summaryRepository.InsertAsync(summary, autoSave: true, ct);
        }

        if (Unchanged(summary, counts))
        {
            return summary;
        }

        Apply(summary, counts);
        return await summaryRepository.UpdateAsync(summary, autoSave: true, ct);
    }

    /// <inheritdoc />
    public async Task<int> RecalculateAllAsync(CancellationToken ct = default)
    {
        var companies = await companyRepository.GetListAsync(cancellationToken: ct);
        if (companies.Count == 0)
        {
            return 0;
        }

        var employees = await employeeRepository.GetListAsync(cancellationToken: ct);
        var equipment = await equipmentRepository.GetListAsync(cancellationToken: ct);
        var progress = await progressRepository.GetListAsync(cancellationToken: ct);
        var summaries = await summaryRepository.GetListAsync(cancellationToken: ct);

        var healthTrainingIds = await HealthTrainingIdsAsync(ct);

        var progressByEmployee = Group(progress);

        var employeesByCompany = employees
            .Where(employee => employee.IsActive)
            .GroupBy(employee => employee.CompanyId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var equipmentByCompany = equipment
            .GroupBy(item => item.CompanyId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var summaryByCompany = summaries
            .GroupBy(summary => summary.CompanyId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(s => s.Id).First());

        var today = Today;
        var inserts = new List<CompanyComplianceSummary>();
        var updates = new List<CompanyComplianceSummary>();

        foreach (var company in companies)
        {
            var counts = Calculate(
                employeesByCompany.GetValueOrDefault(company.Id, []),
                equipmentByCompany.GetValueOrDefault(company.Id, []),
                progressByEmployee,
                healthTrainingIds,
                today);

            if (summaryByCompany.TryGetValue(company.Id, out var summary))
            {
                if (Unchanged(summary, counts))
                {
                    continue;
                }

                Apply(summary, counts);
                updates.Add(summary);
                continue;
            }

            summary = new CompanyComplianceSummary
            {
                CompanyId = company.Id,
                TenantId = company.TenantId
            };

            Apply(summary, counts);
            inserts.Add(summary);
        }

        if (inserts.Count > 0)
        {
            await summaryRepository.InsertManyAsync(inserts, autoSave: true, ct);
        }

        if (updates.Count > 0)
        {
            await summaryRepository.UpdateManyAsync(updates, autoSave: true, ct);
        }

        return inserts.Count + updates.Count;
    }

    // ------------------------------------------------------------------ internals

    private DateTime Today => clock.Today.ToDateTime(TimeOnly.MinValue);

    private Task<CompanyComplianceSummary?> FindSummaryAsync(int companyId, CancellationToken ct)
        => summaryRepository.FindAsync(summary => summary.CompanyId == companyId, ct);

    /// <summary>
    /// Health subjects count towards the health figures, everything else towards the safety
    /// figures — the split the legacy customer portal made.
    /// </summary>
    private async Task<HashSet<int>> HealthTrainingIdsAsync(CancellationToken ct)
    {
        var trainings = await trainingRepository.GetListAsync(
            training => training.TopicGroup == TrainingSubjectGroup.HealthSubjects, ct);

        return trainings.Select(training => training.Id).ToHashSet();
    }

    private static Dictionary<int, List<EmployeeTrainingProgress>> Group(
        List<EmployeeTrainingProgress> progress)
        => progress
            .Where(row => row.IsActive)
            .GroupBy(row => row.CompanyEmployeeId)
            .ToDictionary(group => group.Key, group => group.ToList());

    private static Counts Calculate(
        List<CompanyEmployee> employees,
        List<Equipment> equipment,
        Dictionary<int, List<EmployeeTrainingProgress>> progressByEmployee,
        HashSet<int> healthTrainingIds,
        DateTime today)
    {
        var safetyNone = 0;
        var safetyMissing = 0;
        var healthNone = 0;
        var healthMissing = 0;
        var examinationMissing = 0;

        foreach (var employee in employees)
        {
            var rows = progressByEmployee.GetValueOrDefault(employee.Id, []);

            var safetyRows = rows.FindAll(row => !healthTrainingIds.Contains(row.TrainingId));
            var healthRows = rows.FindAll(row => healthTrainingIds.Contains(row.TrainingId));

            // "None" and "incomplete" are exclusive: somebody who has had no training at all is
            // counted once, as none, rather than twice.
            if (safetyRows.Count == 0)
            {
                safetyNone++;
            }
            else if (safetyRows.Exists(row => !row.LatestTestCompleted))
            {
                safetyMissing++;
            }

            if (healthRows.Count == 0)
            {
                healthNone++;
            }
            else if (healthRows.Exists(row => !row.LatestTestCompleted))
            {
                healthMissing++;
            }

            // Never examined, or the follow-up examination has fallen due.
            if (employee.PreEmploymentExaminationDate is null
                || (employee.PreEmploymentNextExaminationDate is { } next && next < today))
            {
                examinationMissing++;
            }
        }

        // The same rule IEquipmentRepository.GetExaminationOverdueAsync applies, so the panel and
        // the overdue list can never disagree.
        var equipmentOverdue = equipment.Count(item =>
            item.ExaminationDate is null
            || (item.NextExaminationDate is { } next && next < today));

        return new Counts(
            safetyNone, safetyMissing, healthNone, healthMissing,
            examinationMissing, equipmentOverdue);
    }

    private static bool Unchanged(CompanyComplianceSummary summary, Counts counts)
        => summary.IsSafetyTrainingNoneCount == counts.SafetyTrainingNone
           && summary.IsSafetyTrainingMissingCount == counts.SafetyTrainingMissing
           && summary.IsHealthTrainingNoneCount == counts.HealthTrainingNone
           && summary.IsHealthTrainingMissingCount == counts.HealthTrainingMissing
           && summary.PreEmploymentHealthExaminationMissingCount == counts.PreEmploymentExaminationMissing
           && summary.EquipmentExaminationMissingCount == counts.EquipmentExaminationMissing;

    private void Apply(CompanyComplianceSummary summary, Counts counts)
    {
        summary.IsSafetyTrainingNoneCount = counts.SafetyTrainingNone;
        summary.IsSafetyTrainingMissingCount = counts.SafetyTrainingMissing;
        summary.IsHealthTrainingNoneCount = counts.HealthTrainingNone;
        summary.IsHealthTrainingMissingCount = counts.HealthTrainingMissing;
        summary.PreEmploymentHealthExaminationMissingCount = counts.PreEmploymentExaminationMissing;
        summary.EquipmentExaminationMissingCount = counts.EquipmentExaminationMissing;
        summary.CalculatedTime = clock.Now;
    }
}
