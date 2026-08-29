using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Companies;
using Ensa.Application.Contracts.Companies.Dtos;
using Ensa.Application.Contracts.Companies.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Companies;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Companies;

/// <summary>
/// Application service for the physical/organizational departments of a workplace.
/// <para>
/// <see cref="WorkplaceDepartment"/> has no domain Manager, so this service owns the two
/// rules that apply to it — the department name is unique inside a workplace, and a
/// department that still has employees cannot be removed — and persists through the
/// repository with <c>autoSave: true</c>.
/// </para>
/// </summary>
public class WorkplaceDepartmentAppService(
    IServiceProvider serviceProvider,
    IWorkplaceDepartmentRepository workplaceDepartmentRepository,
    ICompanyRepository companyRepository)
    : EnsaAppService(serviceProvider), IWorkplaceDepartmentAppService
{
    /// <inheritdoc />
    public async Task<WorkplaceDepartmentDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.WorkplaceDepartment.Default);

        var department = await workplaceDepartmentRepository.FindAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(WorkplaceDepartment), id);

        return ObjectMapper.Map<WorkplaceDepartment, WorkplaceDepartmentDto>(department);
    }

    /// <inheritdoc />
    public async Task<WorkplaceDepartmentNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.WorkplaceDepartment.Default);

        var navigation = await workplaceDepartmentRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(WorkplaceDepartment), id);

        return new WorkplaceDepartmentNavigationDto
        {
            WorkplaceDepartment = ObjectMapper
                .Map<WorkplaceDepartment, WorkplaceDepartmentDto>(navigation.WorkplaceDepartment),
            Company = navigation.Company is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Company.Id,
                    DisplayName = navigation.Company.CompanyName,
                    Code = navigation.Company.SsiNumber,
                    IsActive = navigation.Company.IsActive
                },
            Documents = ObjectMapper
                .Map<List<DepartmentDocument>, List<DepartmentDocumentDto>>(navigation.Documents),
            Employees = [.. navigation.Employees.Select(p => new LookupDto
            {
                Id = p.Id,
                DisplayName = $"{p.Name} {p.LastName}".Trim(),
                Code = p.NationalId,
                IsActive = p.IsActive
            })],
            EmployeeCount = await workplaceDepartmentRepository
                .GetEmployeeCountAsync(id, cancellationToken)
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<WorkplaceDepartmentListDto>> GetListAsync(
        GetWorkplaceDepartmentListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.WorkplaceDepartment.Default);

        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var companyId = input.CompanyId;
        var deletable = input.IsDeletable;

        var sorting = NormalizeSorting(input.Sorting, "DepartmentName ASC");

        var total = await workplaceDepartmentRepository.GetCountAsync(
            b => (companyId == null || b.CompanyId == companyId)
                 && (deletable == null || b.IsDeletable == deletable)
                 && (search == null || b.DepartmentName.Contains(search)),
            cancellationToken);

        var records = await workplaceDepartmentRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            b => (companyId == null || b.CompanyId == companyId)
                 && (deletable == null || b.IsDeletable == deletable)
                 && (search == null || b.DepartmentName.Contains(search)),
            cancellationToken);

        var items = ObjectMapper.Map<List<WorkplaceDepartment>, List<WorkplaceDepartmentListDto>>(records);

        await FillCompanyNamesAsync(items, cancellationToken);

        return new PagedResultDto<WorkplaceDepartmentListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetLookupAsync(
        int companyId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.WorkplaceDepartment.Default);

        var records = await workplaceDepartmentRepository.GetListByCompanyAsync(companyId, cancellationToken);

        var result = records
            .Select(b => new LookupDto
            {
                Id = b.Id,
                DisplayName = b.DepartmentName
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<WorkplaceDepartmentDto> CreateAsync(
        CreateWorkplaceDepartmentDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.WorkplaceDepartment.Create);

        var department = ObjectMapper.Map<CreateWorkplaceDepartmentDto, WorkplaceDepartment>(input);
        department.DepartmentName = department.DepartmentName.Trim();

        await EnsureCompanyExistsAsync(department.CompanyId, cancellationToken);
        await EnsureDepartmentNameIsUniqueAsync(department, exceptId: null, cancellationToken);

        // No Manager for this entity — the repository persists it directly.
        department = await workplaceDepartmentRepository.InsertAsync(department, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Workplace department created: {DepartmentId} — {DepartmentName} (company {CompanyId})",
            department.Id, department.DepartmentName, department.CompanyId);

        return ObjectMapper.Map<WorkplaceDepartment, WorkplaceDepartmentDto>(department);
    }

    /// <inheritdoc />
    public async Task<WorkplaceDepartmentDto> UpdateAsync(
        int id,
        UpdateWorkplaceDepartmentDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.WorkplaceDepartment.Update);

        var department = await workplaceDepartmentRepository.FindAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(WorkplaceDepartment), id);

        ObjectMapper.Map(input, department);
        department.DepartmentName = department.DepartmentName.Trim();

        await EnsureCompanyExistsAsync(department.CompanyId, cancellationToken);
        await EnsureDepartmentNameIsUniqueAsync(department, exceptId: id, cancellationToken);

        department = await workplaceDepartmentRepository.UpdateAsync(department, autoSave: true, cancellationToken);

        return ObjectMapper.Map<WorkplaceDepartment, WorkplaceDepartmentDto>(department);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.WorkplaceDepartment.Delete);

        var department = await workplaceDepartmentRepository.FindAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(WorkplaceDepartment), id);

        if (!department.IsDeletable)
        {
            throw new BusinessException(
                    "This department was created by the system and cannot be removed.",
                    "Ensa:WorkplaceDepartment:NotDeletable")
                .WithData("DepartmentName", department.DepartmentName);
        }

        var employeeCount = await workplaceDepartmentRepository.GetEmployeeCountAsync(id, cancellationToken);
        if (employeeCount > 0)
        {
            throw new BusinessException(
                    "The department still has employees assigned to it and cannot be removed.",
                    "Ensa:WorkplaceDepartment:HasAssignedEmployees")
                .WithData("DepartmentName", department.DepartmentName)
                .WithData("EmployeeCount", employeeCount);
        }

        await workplaceDepartmentRepository.DeleteAsync(department, autoSave: true, cancellationToken);

        Logger.LogInformation("Workplace department deleted: {DepartmentId}", id);
    }

    // -----------------------------------------------------------------

    /// <summary>The department name must be unique inside its workplace.</summary>
    private async Task EnsureDepartmentNameIsUniqueAsync(
        WorkplaceDepartment department,
        int? exceptId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(department.DepartmentName))
        {
            throw new EnsaValidationException(
                nameof(WorkplaceDepartment.DepartmentName),
                "The department name cannot be empty.");
        }

        var exists = await workplaceDepartmentRepository.DepartmentNameExistsAsync(
            department.DepartmentName,
            department.CompanyId,
            exceptId,
            cancellationToken);

        if (exists)
        {
            throw new BusinessException(
                    "Department name must be unique within the workplace.",
                    "Ensa:WorkplaceDepartment:NameAlreadyUsed")
                .WithData("DepartmentName", department.DepartmentName);
        }
    }

    /// <summary>The workplace the department is attached to must exist in the active tenant.</summary>
    private async Task EnsureCompanyExistsAsync(int companyId, CancellationToken cancellationToken)
    {
        if (companyId <= 0)
        {
            throw new EnsaValidationException(
                nameof(WorkplaceDepartment.CompanyId),
                "A workplace must be selected for the department.");
        }

        _ = await companyRepository.FindAsync(companyId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Company), companyId);
    }

    /// <summary>
    /// Resolves the workplace names of the listed departments with one extra query instead of
    /// a join, keeping the Application layer free of EF Core.
    /// </summary>
    private async Task FillCompanyNamesAsync(
        List<WorkplaceDepartmentListDto> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        var companyIds = items.Select(b => b.CompanyId).Distinct().ToList();

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
