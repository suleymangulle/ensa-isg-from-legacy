using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Companies;
using Ensa.Application.Contracts.Companies.Dtos;
using Ensa.Application.Contracts.Companies.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Companies;
using Ensa.Domain.Companies.Navigations;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Companies;

/// <summary>
/// Application service for the employees of a served workplace.
/// <para>
/// Business rules — national-id checksum, per-company uniqueness, the "one person cannot be
/// active at two workplaces" rule and the date consistency checks — live in
/// <see cref="CompanyEmployeeManager"/>. The Manager also <b>persists</b> the entity, so this
/// service never calls <c>InsertAsync</c>/<c>UpdateAsync</c> after a Manager call.
/// </para>
/// <para>
/// No <c>try/catch</c> here — <c>EnsaExceptionFilter</c> shapes the response. The tenant
/// filter is applied by the global query filter in <c>EnsaDbContext</c>.
/// </para>
/// </summary>
public class CompanyEmployeeAppService(
    IServiceProvider serviceProvider,
    ICompanyEmployeeRepository companyEmployeeRepository,
    ICompanyRepository companyRepository,
    ICompanyEmployeeManager companyEmployeeManager)
    : EnsaAppService(serviceProvider), ICompanyEmployeeAppService
{
    /// <summary>Maximum number of records returned by a drop-down query.</summary>
    private const int LookupMaxRecord = 50;

    /// <inheritdoc />
    public async Task<CompanyEmployeeDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CompanyEmployee.Default);

        var employee = await companyEmployeeRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(CompanyEmployee), id);

        return ObjectMapper.Map<CompanyEmployee, CompanyEmployeeDto>(employee);
    }

    /// <inheritdoc />
    public async Task<CompanyEmployeeNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CompanyEmployee.Default);

        var navigation = await companyEmployeeRepository.GetWithNavigationAsync(
                             id,
                             includeHealthInfo: true,
                             cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(CompanyEmployee), id);

        return new CompanyEmployeeNavigationDto
        {
            CompanyEmployee = ObjectMapper.Map<CompanyEmployee, CompanyEmployeeDto>(navigation.CompanyEmployee),
            Company = navigation.Company is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Company.Id,
                    DisplayName = navigation.Company.CompanyName,
                    Code = navigation.Company.SsiNumber,
                    IsActive = navigation.Company.IsActive
                },
            AssignedDepartment = navigation.AssignedDepartment is null
                ? null
                : new LookupDto
                {
                    Id = navigation.AssignedDepartment.Id,
                    DisplayName = navigation.AssignedDepartment.DepartmentName
                },
            HealthInfo = navigation.HealthInfo is null
                ? null
                : ObjectMapper.Map<EmployeeHealthInfo, EmployeeHealthInfoDto>(navigation.HealthInfo),
            Immunizations = ObjectMapper
                .Map<List<EmployeeImmunization>, List<EmployeeImmunizationDto>>(navigation.Immunizations),
            FamilyHistory = ObjectMapper
                .Map<List<EmployeeFamilyHistory>, List<EmployeeFamilyHistoryDto>>(navigation.FamilyHistory),
            WorkHistory = ObjectMapper
                .Map<List<EmployeeWorkHistory>, List<EmployeeWorkHistoryDto>>(navigation.WorkHistory),
            Duties = ObjectMapper
                .Map<List<CompanyEmployeeDuty>, List<CompanyEmployeeDutyDto>>(navigation.Duties),
            LatestTrainings = ObjectMapper
                .Map<List<EmployeeLatestTrainingInfo>, List<EmployeeLatestTrainingInfoDto>>(navigation.LatestTrainings)
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<CompanyEmployeeListDto>> GetListAsync(
        GetCompanyEmployeeListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.CompanyEmployee.Default);

        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var companyId = input.CompanyId;
        var departmentId = input.DepartmentId;
        var isActive = input.IsActive;
        var gender = input.Gender;

        // The captured locals are compared against null inside the expression so that a single
        // predicate covers every combination; EF folds the null branches away at translation time.
        var sorting = NormalizeSorting(input.Sorting, "LastName ASC, Name ASC");

        var total = await companyEmployeeRepository.GetCountAsync(
            p => (companyId == null || p.CompanyId == companyId)
                 && (departmentId == null || p.AssignedDepartmentId == departmentId)
                 && (isActive == null || p.IsActive == isActive)
                 && (gender == null || p.Gender == gender)
                 && (search == null
                     || p.Name.Contains(search)
                     || p.LastName.Contains(search)
                     || (p.NationalId != null && p.NationalId.Contains(search))),
            cancellationToken);

        var records = await companyEmployeeRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            p => (companyId == null || p.CompanyId == companyId)
                 && (departmentId == null || p.AssignedDepartmentId == departmentId)
                 && (isActive == null || p.IsActive == isActive)
                 && (gender == null || p.Gender == gender)
                 && (search == null
                     || p.Name.Contains(search)
                     || p.LastName.Contains(search)
                     || (p.NationalId != null && p.NationalId.Contains(search))),
            cancellationToken);

        var items = ObjectMapper.Map<List<CompanyEmployee>, List<CompanyEmployeeListDto>>(records);

        await FillCompanyNamesAsync(items, cancellationToken);

        return new PagedResultDto<CompanyEmployeeListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetLookupAsync(
        int? companyId = null,
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CompanyEmployee.Default);

        var search = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();

        var records = await companyEmployeeRepository.GetPagedListAsync(
            skipCount: 0,
            maxResultCount: LookupMaxRecord,
            sorting: "LastName ASC, Name ASC",
            predicate: p => p.IsActive
                            && (companyId == null || p.CompanyId == companyId)
                            && (search == null
                                || p.Name.Contains(search)
                                || p.LastName.Contains(search)
                                || (p.NationalId != null && p.NationalId.Contains(search))),
            cancellationToken);

        var result = records
            .Select(p => new LookupDto
            {
                Id = p.Id,
                DisplayName = $"{p.Name} {p.LastName}".Trim(),
                Code = p.NationalId,
                IsActive = p.IsActive
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<CompanyEmployeeDto> CreateAsync(
        CreateCompanyEmployeeDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.CompanyEmployee.Create);

        var employee = ObjectMapper.Map<CreateCompanyEmployeeDto, CompanyEmployee>(input);

        // A record created with an exit date is terminated by definition.
        employee.IsActive = employee.TerminationDate is null;

        // Validation AND persistence are the Manager's responsibility. InsertAsync must NOT be
        // called here as well — the entity would be inserted twice and SQL Server would fail
        // with "IDENTITY_INSERT is set to OFF".
        employee = await companyEmployeeManager.CreateAsync(employee, cancellationToken);

        Logger.LogInformation(
            "Company employee created: {EmployeeId} — {FirstName} {LastName} (company {CompanyId})",
            employee.Id, employee.Name, employee.LastName, employee.CompanyId);

        return ObjectMapper.Map<CompanyEmployee, CompanyEmployeeDto>(employee);
    }

    /// <inheritdoc />
    public async Task<CompanyEmployeeDto> UpdateAsync(
        int id,
        UpdateCompanyEmployeeDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.CompanyEmployee.Update);

        var employee = await companyEmployeeRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(CompanyEmployee), id);

        ObjectMapper.Map(input, employee);

        // Persistence happens inside the Manager; no UpdateAsync call here.
        employee = await companyEmployeeManager.UpdateAsync(employee, cancellationToken);

        return ObjectMapper.Map<CompanyEmployee, CompanyEmployeeDto>(employee);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CompanyEmployee.Delete);

        var employee = await companyEmployeeRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(CompanyEmployee), id);

        await companyEmployeeRepository.DeleteAsync(employee, autoSave: true, cancellationToken);

        Logger.LogInformation("Company employee deleted: {EmployeeId}", id);
    }

    /// <inheritdoc />
    public async Task<CompanyEmployeeDto> TerminateAsync(
        int id,
        DateTime exitDate,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CompanyEmployee.Update);

        var employee = await companyEmployeeRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(CompanyEmployee), id);

        // The Manager validates the exit date and saves the record itself.
        await companyEmployeeManager.TerminateAsync(employee, exitDate, cancellationToken);

        Logger.LogInformation(
            "Company employee terminated: {EmployeeId} — exit date {ExitDate:yyyy-MM-dd}",
            id, exitDate);

        return ObjectMapper.Map<CompanyEmployee, CompanyEmployeeDto>(employee);
    }

    /// <inheritdoc />
    public async Task<CompanyEmployeeDto> ReinstateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CompanyEmployee.Update);

        var employee = await companyEmployeeRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(CompanyEmployee), id);

        // Re-runs the "one person cannot be active at two workplaces" rule and saves.
        await companyEmployeeManager.ReinstateAsync(employee, cancellationToken);

        Logger.LogInformation("Company employee reinstated: {EmployeeId}", id);

        return ObjectMapper.Map<CompanyEmployee, CompanyEmployeeDto>(employee);
    }

    // -----------------------------------------------------------------

    /// <summary>
    /// Resolves the workplace names of the listed employees with one extra query instead of
    /// a join, keeping the Application layer free of EF Core.
    /// </summary>
    private async Task FillCompanyNamesAsync(
        List<CompanyEmployeeListDto> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        var companyIds = items.Select(p => p.CompanyId).Distinct().ToList();

        var companies = await companyRepository.GetListAsync(
            f => companyIds.Contains(f.Id),
            cancellationToken);

        var namesById = companies.ToDictionary(f => f.Id, f => f.CompanyName);

        foreach (var item in items)
        {
            if (namesById.TryGetValue(item.CompanyId, out var companyName))
            {
                item.CompanyName = companyName;
            }
        }
    }
}
