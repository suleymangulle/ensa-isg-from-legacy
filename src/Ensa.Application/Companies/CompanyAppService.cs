using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Companies;
using Ensa.Application.Contracts.Companies.Dtos;
using Ensa.Application.Contracts.Companies.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Companies;
using Ensa.Domain.Tenancy;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Companies;

/// <summary>
/// Company application service — the <b>reference implementation</b> for the other modules.
/// <para>
/// Points to keep in mind:
/// <list type="bullet">
/// <item>DI registration is automatic (<c>EnsaApplicationModule</c> scans for it); never register
/// the service by hand.</item>
/// <item>Every public method is guarded by <see cref="EnsaAppService.CheckPermissionAsync"/>.</item>
/// <item>Business rules are delegated to <see cref="CompanyManager"/>; the service only
/// orchestrates and maps.</item>
/// <item>Never write <c>try/catch</c> here — <c>EnsaExceptionFilter</c> wraps the call.</item>
/// <item>The tenant filter is applied by the global query filter in <c>EnsaDbContext</c>; never
/// compare <c>TenantId</c> by hand here.</item>
/// </list>
/// </para>
/// </summary>
public class CompanyAppService(
    IServiceProvider serviceProvider,
    ICompanyRepository companyRepository,
    ICompanyEmployeeRepository companyEmployeeRepository,
    ICompanyManager companyManager,
    ICompanyComplianceCalculator complianceCalculator)
    : EnsaAppService(serviceProvider), ICompanyAppService
{
    /// <summary>Maximum number of records returned in a lookup list.</summary>
    private const int LookupMaxRecord = 50;

    /// <inheritdoc />
    public async Task<CompanyDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Company.Default);

        var company = await companyRepository.FindAsync(id, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Company), id);

        return ObjectMapper.Map<Company, CompanyDto>(company);
    }

    /// <inheritdoc />
    public async Task<CompanyNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Company.Default);

        var navigation = await companyRepository.GetWithNavigationAsync(
                             id,
                             includeEmployees: false,
                             includeBranches: true,
                             cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(Company), id);

        var dto = new CompanyNavigationDto
        {
            Company = ObjectMapper.Map<Company, CompanyDto>(navigation.Company),
            City = Lookup(navigation.City?.Id, navigation.City?.CityName),
            District = Lookup(navigation.District?.Id, navigation.District?.DistrictName),
            Neighborhood = Lookup(navigation.Neighborhood?.Id, navigation.Neighborhood?.NeighborhoodName),
            HeadquarterCompany = Lookup(navigation.HeadquarterCompany?.Id, navigation.HeadquarterCompany?.CompanyName),
            Branches = [.. navigation.Branches.Select(s => new LookupDto
            {
                Id = s.Id,
                DisplayName = s.CompanyName,
                IsActive = s.IsActive
            })],
            Departments = [.. navigation.Departments.Select(b => new LookupDto
            {
                Id = b.Id,
                DisplayName = b.DepartmentName
            })],
            ActiveEmployeeCount = await companyEmployeeRepository
                .GetActiveEmployeeCountAsync(id, cancellationToken)
        };

        // The summary is a cache the background job keeps warm. A company created a moment ago has
        // no row yet, and an empty compliance panel reads as "nothing outstanding" rather than
        // "not computed" - so a miss is computed once, here, instead of waiting for the next round.
        var warningSummary = navigation.Warning
                             ?? await complianceCalculator.RecalculateAsync(id, cancellationToken);

        if (warningSummary is { } warning)
        {
            dto.WarningSummary = new CompanyWarningSummaryDto
            {
                IsSafetyTrainingNoneCount = warning.IsSafetyTrainingNoneCount ?? 0,
                IsSafetyTrainingMissingCount = warning.IsSafetyTrainingMissingCount ?? 0,
                IsHealthTrainingNoneCount = warning.IsHealthTrainingNoneCount ?? 0,
                IsHealthTrainingMissingCount = warning.IsHealthTrainingMissingCount ?? 0,
                PreEmploymentHealthExaminationMissingCount = warning.PreEmploymentHealthExaminationMissingCount ?? 0,
                EquipmentExaminationMissingCount = warning.EquipmentExaminationMissingCount ?? 0
            };
        }

        return dto;
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<CompanyListDto>> GetListAsync(
        GetCompanyListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Company.Default);

        var predicate = BuildFilter(input, ResolveOfficeScope(input.OfficeId));
        var sorting = NormalizeSorting(input.Sorting, "CompanyName ASC");

        var total = await companyRepository.GetCountAsync(predicate, cancellationToken);

        var records = await companyRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Company>, List<CompanyListDto>>(records);

        return new PagedResultDto<CompanyListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Company.Default);

        var search = filter?.Trim();

        // The lookup is scoped like the list, and for the same reason the legacy screens scoped
        // theirs: a workplace picker that offers companies from an office the user is not working in
        // sends them to a record their own list does not show.
        Expression<Func<Company, bool>> predicate = string.IsNullOrEmpty(search)
            ? f => f.IsActive
            : f => f.IsActive && f.CompanyName.Contains(search);

        var officeScope = ResolveOfficeScope(requestedOfficeId: null);

        if (officeScope.SingleOfficeId is { } officeId)
        {
            predicate = Combine(predicate, f => f.OfficeId == officeId);
        }
        else if (officeScope.IsRestricted)
        {
            var officeIds = officeScope.OfficeIds;
            predicate = Combine(predicate, f => officeIds.Contains(f.OfficeId));
        }

        var records = await companyRepository.GetPagedListAsync(
            skipCount: 0,
            maxResultCount: LookupMaxRecord,
            sorting: "CompanyName ASC",
            predicate: predicate,
            cancellationToken);

        var result = records
            .Select(f => new LookupDto
            {
                Id = f.Id,
                DisplayName = f.CompanyName,
                Code = f.SsiNumber,
                IsActive = f.IsActive
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<CompanyDto> CreateAsync(
        CreateCompanyDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Company.Create);

        var company = ObjectMapper.Map<CreateCompanyDto, Company>(input);

        // All business rules (SSI number uniqueness, headquarter/branch consistency, NACE code to
        // hazard class agreement, per-tenant company limit) AND persistence are the manager's
        // responsibility. Do NOT call InsertAsync here as well — the same entity would be added
        // twice and SQL Server would fail with "IDENTITY_INSERT is set to OFF".
        company = await companyManager.CreateAsync(company, cancellationToken);

        Logger.LogInformation("Company created: {CompanyId} — {CompanyName}", company.Id, company.CompanyName);

        return ObjectMapper.Map<Company, CompanyDto>(company);
    }

    /// <inheritdoc />
    public async Task<CompanyDto> UpdateAsync(
        int id,
        UpdateCompanyDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Company.Update);

        var company = await companyRepository.FindAsync(id, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Company), id);

        ObjectMapper.Map(input, company);

        // The manager persists the change; do not call UpdateAsync here as well.
        company = await companyManager.UpdateAsync(company, cancellationToken);

        return ObjectMapper.Map<Company, CompanyDto>(company);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Company.Delete);

        var company = await companyRepository.FindAsync(id, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Company), id);

        // A headquarter with active branches cannot be deactivated — the rule lives in the manager,
        // which deactivates and saves the record. The soft delete is applied afterwards.
        await companyManager.DeactivateAsync(company, cancellationToken);

        await companyRepository.DeleteAsync(company, autoSave: true, cancellationToken);

        Logger.LogInformation("Company deleted: {CompanyId}", id);
    }

    // -----------------------------------------------------------------

    /// <summary>
    /// The list filter, including the office restriction.
    /// <para>
    /// <paramref name="officeScope"/> has already reconciled <c>input.OfficeId</c> with the office
    /// the request is running for, so this method never reads <c>input.OfficeId</c> itself — reading
    /// both would be two answers to one question, and the caller-supplied one is the untrusted half.
    /// A workplace belongs to exactly one office (<c>Company.OfficeId</c> is not nullable), which is
    /// the relationship the legacy company list filtered on too.
    /// </para>
    /// </summary>
    private static Expression<Func<Company, bool>>? BuildFilter(
        GetCompanyListInput input,
        OfficeQueryScope officeScope)
    {
        Expression<Func<Company, bool>> predicate = f => true;
        var applied = false;

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var search = input.Filter.Trim();
            predicate = Combine(predicate, f =>
                f.CompanyName.Contains(search)
                || (f.SsiNumber != null && f.SsiNumber.Contains(search))
                || (f.AuthorizedPerson != null && f.AuthorizedPerson.Contains(search)));
            applied = true;
        }

        if (input.HazardClass is { } hazardClass)
        {
            predicate = Combine(predicate, f => f.HazardClass == hazardClass);
            applied = true;
        }

        if (input.CityId is { } cityId)
        {
            predicate = Combine(predicate, f => f.CityId == cityId);
            applied = true;
        }

        if (officeScope.SingleOfficeId is { } officeId)
        {
            predicate = Combine(predicate, f => f.OfficeId == officeId);
            applied = true;
        }
        else if (officeScope.IsRestricted)
        {
            var officeIds = officeScope.OfficeIds;
            predicate = Combine(predicate, f => officeIds.Contains(f.OfficeId));
            applied = true;
        }

        if (input.HeadquarterCompanyId is { } headquarterId)
        {
            predicate = Combine(predicate, f => f.HeadquarterCompanyId == headquarterId);
            applied = true;
        }

        if (input.IsActive is { } active)
        {
            predicate = Combine(predicate, f => f.IsActive == active);
            applied = true;
        }

        return applied ? predicate : null;
    }

    private static Expression<Func<Company, bool>> Combine(
        Expression<Func<Company, bool>> sol,
        Expression<Func<Company, bool>> sag)
    {
        var parameter = Expression.Parameter(typeof(Company), "f");

        var body = Expression.AndAlso(
            new ParameterRebinder(sol.Parameters[0], parameter).Visit(sol.Body)!,
            new ParameterRebinder(sag.Parameters[0], parameter).Visit(sag.Body)!);

        return Expression.Lambda<Func<Company, bool>>(body, parameter);
    }

    private static LookupDto? Lookup(int? id, string? name)
        => id is null ? null : new LookupDto { Id = id.Value, DisplayName = name ?? string.Empty };

    /// <summary>Rebinds the parameters of two separate lambdas onto a single shared parameter.</summary>
    private sealed class ParameterRebinder(ParameterExpression previous, ParameterExpression replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == previous ? replacement : base.VisitParameter(node);
    }
}
