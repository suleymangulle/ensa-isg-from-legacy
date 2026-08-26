using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Health;
using Ensa.Application.Contracts.Health.Dtos;
using Ensa.Application.Contracts.Health.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Companies;
using Ensa.Domain.Health;
using Ensa.Domain.Membership;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Health;

/// <summary>
/// Health surveillance (EK-2) medical examination form application service.
/// <para>
/// <b>PRIVACY.</b> This service handles special-category health data. Two decisions follow
/// from that and are enforced here rather than left to callers:
/// <list type="bullet">
/// <item>Collection-returning methods project to <c>MedicalExaminationFormListDto</c>, which
/// carries no clinical field — a list screen or an export never becomes a bulk disclosure
/// of medical records.</item>
/// <item>Clinical content is reachable only one record at a time, through
/// <see cref="GetAsync"/> or <see cref="GetWithNavigationAsync"/>, both of which log the
/// access so that a disclosure can be reconstructed afterwards.</item>
/// </list>
/// </para>
/// <para>
/// The statutory interval, the next-examination date and the body mass index belong to
/// <see cref="IHealthSurveillanceManager"/>. That manager is a pure calculator: it performs
/// no persistence, so this service saves the entities itself.
/// </para>
/// </summary>
public class MedicalExaminationFormAppService(
    IServiceProvider serviceProvider,
    IMedicalExaminationFormRepository formRepository,
    IHealthSurveillanceManager healthSurveillanceManager,
    IRepository<MedicalExamComplaint> complaintRepository,
    IRepository<MedicalExamPhysicalFinding> physicalFindingRepository,
    IRepository<MedicalExamLabTest> labTestRepository,
    IRepository<MedicalExamHabit> habitRepository,
    IRepository<MedicalExamWorkCondition> workConditionRepository,
    IRepository<MedicalExamImmunization> immunizationRepository,
    IReadOnlyRepository<CompanyEmployee> employeeRepository,
    IReadOnlyRepository<Company> companyRepository,
    IReadOnlyRepository<User> userRepository)
    : EnsaAppService(serviceProvider), IMedicalExaminationFormAppService
{
    /// <summary>Upper bound for the periodic follow-up warning list.</summary>
    private const int ExpiringMaxRecord = 100;

    /// <inheritdoc />
    public async Task<MedicalExaminationFormDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Default);

        var form = await formRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(MedicalExaminationForm), id);

        Logger.LogInformation(
            "Medical examination form read. FormId={FormId}, UserId={UserId}", id, CurrentUser.Id);

        return ObjectMapper.Map<MedicalExaminationForm, MedicalExaminationFormDto>(form);
    }

    /// <inheritdoc />
    public async Task<MedicalExaminationFormNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Default);

        // One repository call brings the form, the employee, the workplace and all six
        // child collections back together — the child rows are not fetched per row.
        var navigation = await formRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(MedicalExaminationForm), id);

        Logger.LogInformation(
            "Medical examination form detail read. FormId={FormId}, UserId={UserId}", id, CurrentUser.Id);

        var previous = await formRepository.GetLatestExaminationAsync(
            navigation.Form.CompanyEmployeeId,
            reportType: null,
            cancellationToken);

        return new MedicalExaminationFormNavigationDto
        {
            Form = ObjectMapper.Map<MedicalExaminationForm, MedicalExaminationFormDto>(navigation.Form),
            // Name and id only. The national id is deliberately left out: it is personal
            // data unrelated to the examination, and pairing it with a health record in one
            // payload widens the disclosure for no clinical benefit.
            Employee = Lookup(
                navigation.Employee?.Id,
                navigation.Employee is null
                    ? null
                    : $"{navigation.Employee.Name} {navigation.Employee.LastName}".Trim()),
            Company = Lookup(navigation.Company?.Id, navigation.Company?.CompanyName, navigation.Company?.SsiNumber),
            PhysicianFullName = navigation.PhysicianFullName,
            Complaints = ObjectMapper.Map<List<MedicalExamComplaint>, List<MedicalExamComplaintDto>>(
                navigation.Complaints),
            PhysicalFindings = ObjectMapper
                .Map<List<MedicalExamPhysicalFinding>, List<MedicalExamPhysicalFindingDto>>(
                    navigation.FizikFindings),
            LabTests = ObjectMapper.Map<List<MedicalExamLabTest>, List<MedicalExamLabTestDto>>(navigation.LabTests),
            Habits = ObjectMapper.Map<List<MedicalExamHabit>, List<MedicalExamHabitDto>>(navigation.Habits),
            WorkConditions = ObjectMapper
                .Map<List<MedicalExamWorkCondition>, List<MedicalExamWorkConditionDto>>(navigation.WorkConditions),
            Immunizations = ObjectMapper
                .Map<List<MedicalExamImmunization>, List<MedicalExamImmunizationDto>>(navigation.Immunizations),
            // Skip the form itself: "previous" means the one before this record.
            PreviousExaminationDate = previous is not null && previous.Id != id
                ? previous.ExaminationDate
                : navigation.PreviousExaminationDate,
            IbysQueryNo = navigation.IbysQueryNo
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<MedicalExaminationFormListDto>> GetListAsync(
        GetMedicalExaminationFormListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "ExaminationDate DESC");

        var total = await formRepository.GetCountAsync(predicate, cancellationToken);

        var records = await formRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = await ToListDtosAsync(records, cancellationToken);

        return new PagedResultDto<MedicalExaminationFormListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<MedicalExaminationFormDto?> GetLatestForEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Default);

        var form = await formRepository.GetLatestExaminationAsync(employeeId, reportType: null, cancellationToken);

        return form is null
            ? null
            : ObjectMapper.Map<MedicalExaminationForm, MedicalExaminationFormDto>(form);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<MedicalExaminationFormListDto>> GetExpiringAsync(
        int companyId,
        DateTime asOf,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Default);

        var records = await formRepository.GetDurationExpiredAsync(
            companyId,
            asOf,
            ExpiringMaxRecord,
            cancellationToken);

        var items = await ToListDtosAsync(records, cancellationToken);

        return new ListResultDto<MedicalExaminationFormListDto>(items);
    }

    /// <inheritdoc />
    public async Task<MedicalExaminationFormDto> CreateAsync(
        CreateMedicalExaminationFormDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Create);

        var form = ObjectMapper.Map<CreateMedicalExaminationFormDto, MedicalExaminationForm>(input);

        await ApplyDerivedValuesAsync(form, cancellationToken);

        // IHealthSurveillanceManager only calculates; it does not persist. The form is
        // therefore saved here, exactly once.
        form = await formRepository.InsertAsync(form, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Medical examination form created. FormId={FormId}, EmployeeId={EmployeeId}",
            form.Id, form.CompanyEmployeeId);

        return ObjectMapper.Map<MedicalExaminationForm, MedicalExaminationFormDto>(form);
    }

    /// <inheritdoc />
    public async Task<MedicalExaminationFormDto> UpdateAsync(
        int id,
        UpdateMedicalExaminationFormDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Update);

        var form = await formRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(MedicalExaminationForm), id);

        // A form already accepted by IBYS is the legal record of that notification and
        // must not be edited afterwards.
        if (form.IbysStatus == IbysSubmissionStatus.Approved)
        {
            throw new BusinessException(
                "An examination form that has been approved by IBYS can no longer be edited.",
                "Ensa:Health:FormLockedAfterIbysApproval")
                .WithData("FormId", id);
        }

        ObjectMapper.Map(input, form);

        await ApplyDerivedValuesAsync(form, cancellationToken);

        form = await formRepository.UpdateAsync(form, autoSave: true, cancellationToken);

        return ObjectMapper.Map<MedicalExaminationForm, MedicalExaminationFormDto>(form);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Delete);

        var form = await formRepository.FindAsync(id, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(MedicalExaminationForm), id);

        if (form.IbysStatus == IbysSubmissionStatus.Approved)
        {
            throw new BusinessException(
                "An examination form that has been approved by IBYS can no longer be deleted.",
                "Ensa:Health:FormLockedAfterIbysApproval")
                .WithData("FormId", id);
        }

        // Clinical child rows are removed with the form so that no orphaned health data
        // survives the deletion of the record it belonged to.
        await complaintRepository.DeleteDirectAsync(x => x.MedicalExaminationFormId == id, cancellationToken);
        await physicalFindingRepository.DeleteDirectAsync(x => x.MedicalExaminationFormId == id, cancellationToken);
        await labTestRepository.DeleteDirectAsync(x => x.MedicalExaminationFormId == id, cancellationToken);
        await habitRepository.DeleteDirectAsync(x => x.MedicalExaminationFormId == id, cancellationToken);
        await workConditionRepository.DeleteDirectAsync(x => x.MedicalExaminationFormId == id, cancellationToken);
        await immunizationRepository.DeleteDirectAsync(x => x.MedicalExaminationFormId == id, cancellationToken);

        await formRepository.DeleteAsync(form, autoSave: true, cancellationToken);

        Logger.LogInformation("Medical examination form deleted. FormId={FormId}", id);
    }

    // ------------------------------------------------------------------
    // Child collections — each call replaces the whole set for one form.
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<ListResultDto<MedicalExamComplaintDto>> SaveComplaintsAsync(
        int formId,
        List<SaveMedicalExamComplaintDto> input,
        CancellationToken cancellationToken = default)
    {
        var rows = await ReplaceChildSetAsync(
            formId,
            input,
            x => x.ComplaintType,
            complaintRepository,
            (entity, id) => entity.MedicalExaminationFormId = id,
            x => x.MedicalExaminationFormId == formId,
            "complaint type",
            cancellationToken);

        return new ListResultDto<MedicalExamComplaintDto>(
            ObjectMapper.Map<List<MedicalExamComplaint>, List<MedicalExamComplaintDto>>(rows));
    }

    /// <inheritdoc />
    public async Task<ListResultDto<MedicalExamPhysicalFindingDto>> SavePhysicalFindingsAsync(
        int formId,
        List<SaveMedicalExamPhysicalFindingDto> input,
        CancellationToken cancellationToken = default)
    {
        var rows = await ReplaceChildSetAsync(
            formId,
            input,
            x => x.System,
            physicalFindingRepository,
            (entity, id) => entity.MedicalExaminationFormId = id,
            x => x.MedicalExaminationFormId == formId,
            "body system",
            cancellationToken);

        return new ListResultDto<MedicalExamPhysicalFindingDto>(
            ObjectMapper.Map<List<MedicalExamPhysicalFinding>, List<MedicalExamPhysicalFindingDto>>(rows));
    }

    /// <inheritdoc />
    public async Task<ListResultDto<MedicalExamLabTestDto>> SaveLabTestsAsync(
        int formId,
        List<SaveMedicalExamLabTestDto> input,
        CancellationToken cancellationToken = default)
    {
        var rows = await ReplaceChildSetAsync(
            formId,
            input,
            x => x.LabTestType,
            labTestRepository,
            (entity, id) => entity.MedicalExaminationFormId = id,
            x => x.MedicalExaminationFormId == formId,
            "laboratory test type",
            cancellationToken);

        return new ListResultDto<MedicalExamLabTestDto>(
            ObjectMapper.Map<List<MedicalExamLabTest>, List<MedicalExamLabTestDto>>(rows));
    }

    /// <inheritdoc />
    public async Task<ListResultDto<MedicalExamHabitDto>> SaveHabitsAsync(
        int formId,
        List<SaveMedicalExamHabitDto> input,
        CancellationToken cancellationToken = default)
    {
        var rows = await ReplaceChildSetAsync(
            formId,
            input,
            x => x.HabitType,
            habitRepository,
            (entity, id) => entity.MedicalExaminationFormId = id,
            x => x.MedicalExaminationFormId == formId,
            "habit type",
            cancellationToken);

        return new ListResultDto<MedicalExamHabitDto>(
            ObjectMapper.Map<List<MedicalExamHabit>, List<MedicalExamHabitDto>>(rows));
    }

    /// <inheritdoc />
    public async Task<ListResultDto<MedicalExamWorkConditionDto>> SaveWorkConditionsAsync(
        int formId,
        List<SaveMedicalExamWorkConditionDto> input,
        CancellationToken cancellationToken = default)
    {
        var rows = await ReplaceChildSetAsync(
            formId,
            input,
            x => x.ConditionType,
            workConditionRepository,
            (entity, id) => entity.MedicalExaminationFormId = id,
            x => x.MedicalExaminationFormId == formId,
            "working condition",
            cancellationToken);

        return new ListResultDto<MedicalExamWorkConditionDto>(
            ObjectMapper.Map<List<MedicalExamWorkCondition>, List<MedicalExamWorkConditionDto>>(rows));
    }

    /// <inheritdoc />
    public async Task<ListResultDto<MedicalExamImmunizationDto>> SaveImmunizationsAsync(
        int formId,
        List<SaveMedicalExamImmunizationDto> input,
        CancellationToken cancellationToken = default)
    {
        var rows = await ReplaceChildSetAsync(
            formId,
            input,
            x => x.ImmunizationType,
            immunizationRepository,
            (entity, id) => entity.MedicalExaminationFormId = id,
            x => x.MedicalExaminationFormId == formId,
            "immunisation type",
            cancellationToken);

        return new ListResultDto<MedicalExamImmunizationDto>(
            ObjectMapper.Map<List<MedicalExamImmunization>, List<MedicalExamImmunizationDto>>(rows));
    }

    // -----------------------------------------------------------------

    /// <summary>
    /// Replaces the whole child set of one form in a single call: validates the form, rejects
    /// duplicate keys (each child table is unique on form + type), removes the previous rows
    /// and inserts the new ones.
    /// </summary>
    private async Task<List<TEntity>> ReplaceChildSetAsync<TInput, TEntity, TKey>(
        int formId,
        List<TInput> input,
        Func<TInput, TKey> keySelector,
        IRepository<TEntity> repository,
        Action<TEntity, int> assignFormId,
        Expression<Func<TEntity, bool>> ownedByForm,
        string keyDescription,
        CancellationToken cancellationToken)
        where TEntity : class, Ensa.Domain.Common.IEntity<int>
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.MedicalExamination.Update);

        _ = await formRepository.FindAsync(formId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(MedicalExaminationForm), formId);

        var duplicate = input
            .GroupBy(keySelector)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            throw new BusinessException(
                $"The examination form may hold only one row per {keyDescription}.",
                "Ensa:Health:DuplicateChildEntry")
                .WithData("Kind", keyDescription)
                .WithData("Value", duplicate.Key);
        }

        // Physical delete: the child tables are unique on (form, type), so leaving
        // soft-deleted rows behind would collide with the replacement rows.
        await repository.DeleteDirectAsync(ownedByForm, cancellationToken);

        var rows = new List<TEntity>(input.Count);

        foreach (var item in input)
        {
            var entity = ObjectMapper.Map<TInput, TEntity>(item);
            assignFormId(entity, formId);
            rows.Add(entity);
        }

        if (rows.Count > 0)
        {
            await repository.InsertManyAsync(rows, autoSave: true, cancellationToken);
        }

        Logger.LogInformation(
            "Examination form child set replaced. FormId={FormId}, Kind={Kind}, Count={Count}",
            formId, keyDescription, rows.Count);

        return rows;
    }

    /// <summary>
    /// Applies the values owned by <see cref="IHealthSurveillanceManager"/>: the body mass
    /// index and, when the caller left it blank, the statutory validity end date.
    /// </summary>
    private async Task ApplyDerivedValuesAsync(
        MedicalExaminationForm form,
        CancellationToken cancellationToken)
    {
        form.BodyMassIndex = form is { HeightCm: > 0, WeightKg: > 0 }
            ? healthSurveillanceManager.CalculateBmi(form.HeightCm.Value, form.WeightKg.Value)
            : null;

        if (form.ValidityDate is not null || form.CompanyId is not { } companyId)
        {
            return;
        }

        var company = await companyRepository.FindAsync(companyId, cancellationToken);

        // The manager rejects an unspecified hazard class; without one there is no statutory
        // interval to apply, so the validity date simply stays blank.
        if (company is not null && company.HazardClass != HazardClass.Unspecified)
        {
            form.ValidityDate = healthSurveillanceManager.CalculateNextExaminationDate(
                form.ExaminationDate,
                company.HazardClass);
        }
    }

    /// <summary>
    /// Projects forms to the clinical-free list row, resolving employee, workplace and
    /// physician names with one batched query each — never one query per row.
    /// </summary>
    private async Task<List<MedicalExaminationFormListDto>> ToListDtosAsync(
        List<MedicalExaminationForm> records,
        CancellationToken cancellationToken)
    {
        var items = ObjectMapper.Map<List<MedicalExaminationForm>, List<MedicalExaminationFormListDto>>(records);

        if (items.Count == 0)
        {
            return items;
        }

        var employeeIds = records.Select(f => f.CompanyEmployeeId).Distinct().ToList();
        var companyIds = records.Where(f => f.CompanyId.HasValue).Select(f => f.CompanyId!.Value).Distinct().ToList();
        var physicianIds = records
            .Where(f => f.PhysicianUserId.HasValue)
            .Select(f => f.PhysicianUserId!.Value)
            .Distinct()
            .ToList();

        List<CompanyEmployee> employees = employeeIds.Count == 0
            ? []
            : await employeeRepository.GetListAsync(e => employeeIds.Contains(e.Id), cancellationToken);

        List<Company> companies = companyIds.Count == 0
            ? []
            : await companyRepository.GetListAsync(c => companyIds.Contains(c.Id), cancellationToken);

        List<User> physicians = physicianIds.Count == 0
            ? []
            : await userRepository.GetListAsync(u => physicianIds.Contains(u.Id), cancellationToken);

        var employeeNames = employees.ToDictionary(e => e.Id, e => $"{e.Name} {e.LastName}".Trim());
        var companyNames = companies.ToDictionary(c => c.Id, c => c.CompanyName);
        var physicianNames = physicians.ToDictionary(u => u.Id, u => $"{u.Name} {u.LastName}".Trim());

        foreach (var item in items)
        {
            if (employeeNames.TryGetValue(item.CompanyEmployeeId, out var employeeName))
            {
                item.EmployeeFullName = employeeName;
            }

            if (item.CompanyId is { } companyId && companyNames.TryGetValue(companyId, out var companyName))
            {
                item.CompanyName = companyName;
            }

            if (item.PhysicianUserId is { } physicianId && physicianNames.TryGetValue(physicianId, out var name))
            {
                item.PhysicianFullName = name;
            }
        }

        return items;
    }

    private static Expression<Func<MedicalExaminationForm, bool>>? BuildFilter(
        GetMedicalExaminationFormListInput input)
    {
        Expression<Func<MedicalExaminationForm, bool>> predicate = f => true;
        var applied = false;

        if (input.CompanyId is { } companyId)
        {
            predicate = Combine(predicate, f => f.CompanyId == companyId);
            applied = true;
        }

        if (input.CompanyEmployeeId is { } employeeId)
        {
            predicate = Combine(predicate, f => f.CompanyEmployeeId == employeeId);
            applied = true;
        }

        if (input.PhysicianUserId is { } physicianId)
        {
            predicate = Combine(predicate, f => f.PhysicianUserId == physicianId);
            applied = true;
        }

        if (input.ReportType is { } reportType)
        {
            predicate = Combine(predicate, f => f.ReportType == reportType);
            applied = true;
        }

        if (input.Opinion is { } opinion)
        {
            predicate = Combine(predicate, f => f.Opinion == opinion);
            applied = true;
        }

        if (input.IbysStatus is { } ibysStatus)
        {
            predicate = Combine(predicate, f => f.IbysStatus == ibysStatus);
            applied = true;
        }

        if (input.ExaminationDateFrom is { } from)
        {
            predicate = Combine(predicate, f => f.ExaminationDate >= from);
            applied = true;
        }

        if (input.ExaminationDateTo is { } to)
        {
            predicate = Combine(predicate, f => f.ExaminationDate <= to);
            applied = true;
        }

        // Free-text search never reaches clinical columns: matching a health record by its
        // symptoms would let a caller mine the table without opening a single record.
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var search = input.Filter.Trim();
            predicate = Combine(predicate, f =>
                (f.IbysGroupCode != null && f.IbysGroupCode.Contains(search))
                || (f.Source != null && f.Source.Contains(search)));
            applied = true;
        }

        return applied ? predicate : null;
    }

    private static Expression<Func<MedicalExaminationForm, bool>> Combine(
        Expression<Func<MedicalExaminationForm, bool>> left,
        Expression<Func<MedicalExaminationForm, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(MedicalExaminationForm), "f");

        var body = Expression.AndAlso(
            new ParameterRebinder(left.Parameters[0], parameter).Visit(left.Body)!,
            new ParameterRebinder(right.Parameters[0], parameter).Visit(right.Body)!);

        return Expression.Lambda<Func<MedicalExaminationForm, bool>>(body, parameter);
    }

    private static LookupDto? Lookup(int? id, string? name, string? code = null)
        => id is null
            ? null
            : new LookupDto { Id = id.Value, DisplayName = name ?? string.Empty, Code = code };

    /// <summary>Rewrites two separate lambdas onto a single shared parameter.</summary>
    private sealed class ParameterRebinder(ParameterExpression previous, ParameterExpression replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == previous ? replacement : base.VisitParameter(node);
    }
}
