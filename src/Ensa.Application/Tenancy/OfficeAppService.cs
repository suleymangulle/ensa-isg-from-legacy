using Ensa.Domain.Repositories;
using Ensa.Domain.Companies;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Tenancy;
using Ensa.Application.Contracts.Tenancy.Dtos;
using Ensa.Application.Contracts.Tenancy.Dtos.Navigations;
using Ensa.Domain.Finance;
using Ensa.Domain.Shared.Exceptions;
using Ensa.Domain.Tenancy;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Tenancy;

/// <summary>
/// Application service for the physical offices/branches of an organization.
/// <para>
/// <see cref="Office"/> has no domain Manager, so this service owns the single structural
/// rule that applies to it — an organization has exactly one headquarters office — and
/// persists through the repository with <c>autoSave: true</c>.
/// </para>
/// <para>
/// The tenant filter comes from the global query filter in <c>EnsaDbContext</c>; no manual
/// <c>TenantId</c> comparison happens here.
/// </para>
/// </summary>
public class OfficeAppService(
    IServiceProvider serviceProvider,
    IOfficeRepository officeRepository,
    ICashRegisterRepository cashRegisterRepository,
    IReadOnlyRepository<Company> companyRepository)
    : EnsaAppService(serviceProvider), IOfficeAppService
{
    /// <summary>Maximum number of records returned by a drop-down query.</summary>
    private const int LookupMaxRecord = 50;

    /// <inheritdoc />
    public async Task<OfficeDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Office.Default);

        var office = await officeRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(Office), id);

        return ObjectMapper.Map<Office, OfficeDto>(office);
    }

    /// <inheritdoc />
    public async Task<OfficeNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Office.Default);

        var navigation = await officeRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(Office), id);

        var office = navigation.Office;

        return new OfficeNavigationDto
        {
            Office = ObjectMapper.Map<Office, OfficeDto>(office),
            Organization = navigation.Organization is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Organization.Id,
                    DisplayName = navigation.Organization.Name,
                    Code = navigation.Organization.Code,
                    IsActive = navigation.Organization.IsActive
                },
            // The navigation carries only the resolved names; the ids come from the root record.
            City = Lookup(office.CityId, navigation.CityName),
            District = Lookup(office.DistrictId, navigation.DistrictName),
            UserCount = navigation.UserCount,
            CashRegisterCount = (int)await cashRegisterRepository.GetCountAsync(
                c => c.OfficeId == id && c.IsActive,
                cancellationToken)
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<OfficeListDto>> GetListAsync(
        GetOfficeListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Office.Default);

        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var cityId = input.CityId;
        var companyId = input.CompanyId;
        var headquarterOffice = input.HeadquarterOffice;
        var isActive = input.IsActive;

        // The captured locals are compared against null inside the expression so that a single
        // predicate covers every combination; EF folds the null branches away at translation time.
        var sorting = NormalizeSorting(input.Sorting, "Name ASC");

        var total = await officeRepository.GetCountAsync(
            o => (cityId == null || o.CityId == cityId)
                 && (companyId == null || o.CompanyId == companyId)
                 && (headquarterOffice == null || o.HeadquarterOffice == headquarterOffice)
                 && (isActive == null || o.IsActive == isActive)
                 && (search == null
                     || o.Name.Contains(search)
                     || (o.AuthorizedPerson != null && o.AuthorizedPerson.Contains(search))),
            cancellationToken);

        var records = await officeRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            o => (cityId == null || o.CityId == cityId)
                 && (companyId == null || o.CompanyId == companyId)
                 && (headquarterOffice == null || o.HeadquarterOffice == headquarterOffice)
                 && (isActive == null || o.IsActive == isActive)
                 && (search == null
                     || o.Name.Contains(search)
                     || (o.AuthorizedPerson != null && o.AuthorizedPerson.Contains(search))),
            cancellationToken);

        var items = ObjectMapper.Map<List<Office>, List<OfficeListDto>>(records);

        if (items.Count > 0)
        {
            // One batched query for the whole page. Without it the screen had to build an
            // id -> name map from the company lookup, which is capped, and fall back to
            // "Company #12" for anything the cap left out.
            // CompanyId is optional: an office can report directly to the organization.
            List<int> companyIds =
                [.. records.Where(o => o.CompanyId is > 0).Select(o => o.CompanyId!.Value).Distinct()];

            if (companyIds.Count > 0)
            {
                var companyNames = (await companyRepository
                        .GetListAsync(c => companyIds.Contains(c.Id), cancellationToken))
                    .ToDictionary(c => c.Id, c => c.CompanyName);

                for (var i = 0; i < items.Count; i++)
                {
                    // The filter above already binds a `companyId` local in this method.
                    if (records[i].CompanyId is { } ownerCompanyId
                        && companyNames.TryGetValue(ownerCompanyId, out var companyName))
                    {
                        items[i].CompanyName = companyName;
                    }
                }
            }
        }

        return new PagedResultDto<OfficeListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Office.Default);

        var search = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();

        var records = await officeRepository.GetPagedListAsync(
            skipCount: 0,
            maxResultCount: LookupMaxRecord,
            sorting: "Name ASC",
            predicate: o => o.IsActive && (search == null || o.Name.Contains(search)),
            cancellationToken);

        var result = records
            .Select(o => new LookupDto
            {
                Id = o.Id,
                DisplayName = o.Name,
                IsActive = o.IsActive
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<OfficeDto> CreateAsync(
        CreateOfficeDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Office.Create);

        var office = ObjectMapper.Map<CreateOfficeDto, Office>(input);
        office.Name = office.Name.Trim();

        await EnsureSingleHeadquarterOfficeAsync(office, exceptId: null, cancellationToken);

        // No Manager for this entity — the repository persists it directly.
        office = await officeRepository.InsertAsync(office, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Office created: {OfficeId} — {OfficeName} (headquarters: {IsHeadquarter})",
            office.Id, office.Name, office.HeadquarterOffice);

        return ObjectMapper.Map<Office, OfficeDto>(office);
    }

    /// <inheritdoc />
    public async Task<OfficeDto> UpdateAsync(
        int id,
        UpdateOfficeDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Office.Update);

        var office = await officeRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(Office), id);

        ObjectMapper.Map(input, office);
        office.Name = office.Name.Trim();

        await EnsureSingleHeadquarterOfficeAsync(office, exceptId: id, cancellationToken);

        office = await officeRepository.UpdateAsync(office, autoSave: true, cancellationToken);

        return ObjectMapper.Map<Office, OfficeDto>(office);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Office.Delete);

        var office = await officeRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(Office), id);

        await officeRepository.DeleteAsync(office, autoSave: true, cancellationToken);

        Logger.LogInformation("Office deleted: {OfficeId}", id);
    }

    // -----------------------------------------------------------------

    /// <summary>
    /// An organization may flag exactly one headquarters office. The check runs only when the
    /// incoming record claims the flag; clearing it is always allowed.
    /// </summary>
    private async Task EnsureSingleHeadquarterOfficeAsync(
        Office office,
        int? exceptId,
        CancellationToken cancellationToken)
    {
        if (!office.HeadquarterOffice)
        {
            return;
        }

        // The repository query is already scoped to the active tenant.
        var existing = await officeRepository.FindHeadquarterOfficeAsync(cancellationToken);

        if (existing is null || existing.Id == exceptId)
        {
            return;
        }

        throw new BusinessException(
                "The organization already has a headquarters office.",
                "Ensa:Office:HeadquarterAlreadyExists")
            .WithData("OfficeName", existing.Name);
    }

    private static LookupDto? Lookup(int? id, string? name)
        => id is null ? null : new LookupDto { Id = id.Value, DisplayName = name ?? string.Empty };
}
