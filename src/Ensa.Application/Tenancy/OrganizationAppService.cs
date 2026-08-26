using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Tenancy;
using Ensa.Application.Contracts.Tenancy.Dtos;
using Ensa.Application.Contracts.Tenancy.Dtos.Navigations;
using Ensa.Domain.Shared.Exceptions;
using Ensa.Domain.Tenancy;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Tenancy;

/// <summary>
/// Application service for organizations (tenants).
/// <para>
/// <see cref="Organization"/> is a <b>host</b> entity — it does not implement
/// <c>IMultiTenant</c>, so no global tenant filter applies to it. Every method is therefore
/// guarded by <c>EnsaPermissions.Tenant.*</c>, which only system administrators hold.
/// </para>
/// <para>
/// There is no domain Manager for this entity, so the one invariant that matters — the
/// organization code is unique across the whole installation — is enforced here and the
/// repository persists with <c>autoSave: true</c>.
/// </para>
/// </summary>
public class OrganizationAppService(
    IServiceProvider serviceProvider,
    IOrganizationRepository organizationRepository)
    : EnsaAppService(serviceProvider), IOrganizationAppService
{
    /// <summary>Maximum number of records returned by a drop-down query.</summary>
    private const int LookupMaxRecord = 50;

    /// <inheritdoc />
    public async Task<OrganizationDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Tenant.Default);

        var organization = await organizationRepository.FindAsync(id, cancellationToken)
                           ?? throw new EntityNotFoundException(typeof(Organization), id);

        return ObjectMapper.Map<Organization, OrganizationDto>(organization);
    }

    /// <inheritdoc />
    public async Task<OrganizationNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Tenant.Default);

        var navigation = await organizationRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(Organization), id);

        var organization = navigation.Organization;

        return new OrganizationNavigationDto
        {
            Organization = ObjectMapper.Map<Organization, OrganizationDto>(organization),
            OrganizationType = navigation.OrganizationType is null
                ? null
                : new LookupDto
                {
                    Id = navigation.OrganizationType.Id,
                    DisplayName = navigation.OrganizationType.Name,
                    Code = navigation.OrganizationType.Code,
                    IsActive = navigation.OrganizationType.IsActive
                },
            SubscriptionPlan = navigation.SubscriptionPlan is null
                ? null
                : new LookupDto
                {
                    Id = navigation.SubscriptionPlan.Id,
                    DisplayName = navigation.SubscriptionPlan.Name,
                    Code = navigation.SubscriptionPlan.Code,
                    IsActive = navigation.SubscriptionPlan.IsActive
                },
            // The navigation carries only the resolved names; the ids come from the root record.
            City = Lookup(organization.CityId, navigation.CityName),
            District = Lookup(organization.DistrictId, navigation.DistrictName),
            Offices = [.. navigation.Offices.Select(o => new LookupDto
            {
                Id = o.Id,
                DisplayName = o.Name,
                IsActive = o.IsActive
            })],
            HeadquarterOffice = navigation.HeadquarterOffice is null
                ? null
                : new LookupDto
                {
                    Id = navigation.HeadquarterOffice.Id,
                    DisplayName = navigation.HeadquarterOffice.Name,
                    IsActive = navigation.HeadquarterOffice.IsActive
                },
            CurrentContract = navigation.CurrentContract is null
                ? null
                : ObjectMapper.Map<OrganizationContract, OrganizationContractSummaryDto>(navigation.CurrentContract),
            OfficeCount = navigation.Offices.Count,
            ActiveUserCount = navigation.ActiveUserCount,
            ActiveCompanyCount = navigation.ActiveCompanyCount
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<OrganizationListDto>> GetListAsync(
        GetOrganizationListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Tenant.Default);

        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var organizationTypeId = input.OrganizationTypeId;
        var subscriptionPlanId = input.SubscriptionPlanId;
        var cityId = input.CityId;
        var isActive = input.IsActive;

        // The captured locals are compared against null inside the expression so that a single
        // predicate covers every combination; EF folds the null branches away at translation time.
        var sorting = NormalizeSorting(input.Sorting, "Name ASC");

        var total = await organizationRepository.GetCountAsync(
            k => (organizationTypeId == null || k.OrganizationTypeId == organizationTypeId)
                 && (subscriptionPlanId == null || k.SubscriptionPlanId == subscriptionPlanId)
                 && (cityId == null || k.CityId == cityId)
                 && (isActive == null || k.IsActive == isActive)
                 && (search == null
                     || k.Name.Contains(search)
                     || k.Code.Contains(search)
                     || (k.TaxNumber != null && k.TaxNumber.Contains(search))),
            cancellationToken);

        var records = await organizationRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            k => (organizationTypeId == null || k.OrganizationTypeId == organizationTypeId)
                 && (subscriptionPlanId == null || k.SubscriptionPlanId == subscriptionPlanId)
                 && (cityId == null || k.CityId == cityId)
                 && (isActive == null || k.IsActive == isActive)
                 && (search == null
                     || k.Name.Contains(search)
                     || k.Code.Contains(search)
                     || (k.TaxNumber != null && k.TaxNumber.Contains(search))),
            cancellationToken);

        var items = ObjectMapper.Map<List<Organization>, List<OrganizationListDto>>(records);

        return new PagedResultDto<OrganizationListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Tenant.Default);

        var search = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();

        var records = await organizationRepository.GetPagedListAsync(
            skipCount: 0,
            maxResultCount: LookupMaxRecord,
            sorting: "Name ASC",
            predicate: k => k.IsActive
                            && (search == null || k.Name.Contains(search) || k.Code.Contains(search)),
            cancellationToken);

        var result = records
            .Select(k => new LookupDto
            {
                Id = k.Id,
                DisplayName = k.Name,
                Code = k.Code,
                IsActive = k.IsActive
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<OrganizationDto> CreateAsync(
        CreateOrganizationDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Tenant.Create);

        var organization = ObjectMapper.Map<CreateOrganizationDto, Organization>(input);
        Normalize(organization);

        ValidateSubscriptionPeriod(organization);
        await EnsureCodeIsUniqueAsync(organization.Code, exceptOrganizationId: null, cancellationToken);

        // No Manager for this entity — the repository persists it directly.
        organization = await organizationRepository.InsertAsync(organization, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Organization created: {OrganizationId} — {OrganizationName} ({OrganizationCode})",
            organization.Id, organization.Name, organization.Code);

        return ObjectMapper.Map<Organization, OrganizationDto>(organization);
    }

    /// <inheritdoc />
    public async Task<OrganizationDto> UpdateAsync(
        int id,
        UpdateOrganizationDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Tenant.Update);

        var organization = await organizationRepository.FindAsync(id, cancellationToken)
                           ?? throw new EntityNotFoundException(typeof(Organization), id);

        ObjectMapper.Map(input, organization);
        Normalize(organization);

        ValidateSubscriptionPeriod(organization);
        await EnsureCodeIsUniqueAsync(organization.Code, exceptOrganizationId: id, cancellationToken);

        organization = await organizationRepository.UpdateAsync(organization, autoSave: true, cancellationToken);

        return ObjectMapper.Map<Organization, OrganizationDto>(organization);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Tenant.Delete);

        var organization = await organizationRepository.FindAsync(id, cancellationToken)
                           ?? throw new EntityNotFoundException(typeof(Organization), id);

        // Removing a tenant cuts off every user inside it, so the record is deactivated first
        // and then soft-deleted; the data itself stays recoverable.
        organization.IsActive = false;
        await organizationRepository.UpdateAsync(organization, autoSave: true, cancellationToken);

        await organizationRepository.DeleteAsync(organization, autoSave: true, cancellationToken);

        Logger.LogInformation("Organization deleted: {OrganizationId}", id);
    }

    // -----------------------------------------------------------------

    /// <summary>The code identifies the tenant, so it must be unique installation-wide.</summary>
    private async Task EnsureCodeIsUniqueAsync(
        string code,
        int? exceptOrganizationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new EnsaValidationException(
                nameof(Organization.Code),
                "The organization code cannot be empty.");
        }

        var exists = await organizationRepository.CodeExistsAsync(code, exceptOrganizationId, cancellationToken);
        if (exists)
        {
            throw new BusinessException(
                    "The organization code is already used by another organization.",
                    "Ensa:Organization:CodeAlreadyUsed")
                .WithData("Code", code);
        }
    }

    /// <summary>The subscription end date, when given, must follow the start date.</summary>
    private static void ValidateSubscriptionPeriod(Organization organization)
    {
        if (organization.SubscriptionEnd is { } end && end.Date < organization.SubscriptionStart.Date)
        {
            throw new BusinessException(
                    "The subscription end date cannot precede the start date.",
                    "Ensa:Organization:InvalidSubscriptionPeriod")
                .WithData("SubscriptionStart", organization.SubscriptionStart)
                .WithData("SubscriptionEnd", end);
        }
    }

    private static void Normalize(Organization organization)
    {
        organization.Name = organization.Name.Trim();
        organization.Code = organization.Code.Trim();
        organization.Email = organization.Email?.Trim();
    }

    private static LookupDto? Lookup(int? id, string? name)
        => id is null ? null : new LookupDto { Id = id.Value, DisplayName = name ?? string.Empty };
}
