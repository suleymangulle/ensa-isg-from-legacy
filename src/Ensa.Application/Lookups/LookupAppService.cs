using Ensa.Domain.Menus;
using Ensa.Domain.Finance;
using Ensa.Domain.Common;
using Ensa.Domain.Membership;
using Ensa.Domain.Tenancy;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Lookups;
using Ensa.Application.Contracts.Lookups.Dtos;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Lookups;
using Ensa.Domain.Repositories;

namespace Ensa.Application.Lookups;

/// <summary>
/// Shared reference data behind the drop-downs of every screen.
/// <para>
/// <b>Permission choice.</b> Every endpoint here requires
/// <see cref="EnsaPermissions.Lookups"/>.<c>Default</c> - the view permission of the reference
/// module - and nothing stronger. Two alternatives were rejected:
/// </para>
/// <list type="bullet">
///   <item>
///     Requiring the permission of the calling screen (say <c>Ensa.Company.Create</c> for the
///     province list) is unworkable: the same province list feeds the company form, the user
///     form, the office form and half a dozen reports, so the service would have to guess who
///     is asking, and every new screen would have to be added to that guess.
///   </item>
///   <item>
///     Leaving them open to any authenticated caller was rejected because the NACE catalogue
///     with its hazard classes is curated data, and an unauthenticated-adjacent scrape of the
///     whole table is not something to hand out for free.
///   </item>
/// </list>
/// <para>
/// <c>Lookups.Default</c> sits exactly in between: it is a read-only grant that every role
/// which fills in a form already needs, it carries no customer data, and it keeps a single
/// obvious answer to "may this caller see reference data?".
/// </para>
/// </summary>
public class LookupAppService(
    IServiceProvider serviceProvider,
    ICityRepository cityRepository,
    IRepository<OccupationCode> occupationCodeRepository,
    IRepository<Duty> dutyRepository,
    IRepository<Certificate> certificateRepository,
    IRepository<Period> periodRepository,
    IReadOnlyRepository<OrganizationType> organizationTypeRepository,
    IReadOnlyRepository<SubscriptionPlan> subscriptionPlanRepository,
    IReadOnlyRepository<UserType> userTypeRepository,
    IReadOnlyRepository<PaymentMethod> paymentMethodRepository,
    IReadOnlyRepository<ServiceItem> serviceItemRepository,
    IReadOnlyRepository<MenuType> menuTypeRepository)
    : EnsaAppService(serviceProvider), ILookupAppService
{
    /// <summary>
    /// Upper bound for the NACE search. The catalogue holds thousands of rows, so an
    /// unfiltered request must never stream all of them into a drop-down.
    /// </summary>
    private const int OccupationCodeMaxRecord = 50;

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetCitiesAsync(
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Lookups.Default);

        var cities = await cityRepository.GetListAsync(cancellationToken: cancellationToken);

        var result = cities
            .OrderBy(c => c.CityName, StringComparer.CurrentCulture)
            .Select(c => new LookupDto
            {
                Id = c.Id,
                DisplayName = c.CityName,
                Code = ToInvariant(c.PlateCodeCode)
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetDistrictsAsync(
        int cityId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Lookups.Default);

        var districts = await cityRepository.GetDistrictsAsync(cityId, cancellationToken);

        var result = districts
            .OrderBy(d => d.DistrictName, StringComparer.CurrentCulture)
            .Select(d => new LookupDto
            {
                Id = d.Id,
                DisplayName = d.DistrictName,
                Code = d.DistrictCode is { } code ? ToInvariant(code) : null
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetNeighborhoodsAsync(
        int districtId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Lookups.Default);

        var neighborhoods = await cityRepository.GetNeighborhoodsAsync(districtId, cancellationToken);

        var result = neighborhoods
            .OrderBy(n => n.NeighborhoodName, StringComparer.CurrentCulture)
            .Select(n => new LookupDto
            {
                Id = n.Id,
                DisplayName = n.NeighborhoodName
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<OccupationCodeLookupDto>> GetOccupationCodesAsync(
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Lookups.Default);

        var search = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();

        var records = await occupationCodeRepository.GetPagedListAsync(
            skipCount: 0,
            maxResultCount: OccupationCodeMaxRecord,
            sorting: "NaceCode ASC",
            predicate: o => search == null
                            || o.NaceCode.Contains(search)
                            || o.Tag.Contains(search),
            cancellationToken);

        var result = records
            .Select(o => new OccupationCodeLookupDto
            {
                Id = o.Id,
                DisplayName = o.Tag,
                Code = o.NaceCode,
                Tag = o.Tag,
                HazardClass = o.HazardClass
            })
            .ToList();

        return new ListResultDto<OccupationCodeLookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetDutiesAsync(
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Lookups.Default);

        var duties = await dutyRepository.GetListAsync(d => d.IsActive, cancellationToken);

        var result = duties
            .OrderBy(d => d.DutyName, StringComparer.CurrentCulture)
            .Select(d => new LookupDto
            {
                Id = d.Id,
                DisplayName = d.DutyName,
                Code = d.DutyCode,
                IsActive = d.IsActive
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetCertificatesAsync(
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Lookups.Default);

        var certificates = await certificateRepository.GetListAsync(cancellationToken: cancellationToken);

        var result = certificates
            .OrderBy(c => c.CertificateName, StringComparer.CurrentCulture)
            .Select(c => new LookupDto
            {
                Id = c.Id,
                DisplayName = c.CertificateName,
                Code = c.CertificateCode
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<PeriodLookupDto>> GetPeriodsAsync(
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Lookups.Default);

        var periods = await periodRepository.GetListAsync(cancellationToken: cancellationToken);

        var result = periods
            // Ordered by real duration rather than by name, so "every 3 months" precedes
            // "every 6 months" instead of sorting alphabetically.
            .OrderBy(p => p.PeriodUnit)
            .ThenBy(p => p.PeriodValue)
            .Select(p => new PeriodLookupDto
            {
                Id = p.Id,
                DisplayName = p.PeriodName,
                Code = p.PeriodExpression,
                PeriodValue = p.PeriodValue,
                PeriodUnit = p.PeriodUnit
            })
            .ToList();

        return new ListResultDto<PeriodLookupDto>(result);
    }
    /// <inheritdoc />
    public Task<ListResultDto<LookupDto>> GetOrganizationTypesAsync(
        CancellationToken cancellationToken = default)
        => DefinitionsAsync(organizationTypeRepository, t => t.Name, t => t.Code, t => t.SortOrder, cancellationToken);

    /// <inheritdoc />
    public Task<ListResultDto<LookupDto>> GetSubscriptionPlansAsync(
        CancellationToken cancellationToken = default)
        => DefinitionsAsync(subscriptionPlanRepository, p => p.Name, p => p.Code, p => p.SortOrder, cancellationToken);

    /// <inheritdoc />
    public Task<ListResultDto<LookupDto>> GetUserTypesAsync(
        CancellationToken cancellationToken = default)
        => DefinitionsAsync(userTypeRepository, t => t.Name, t => t.Code, t => t.SortOrder, cancellationToken);

    /// <summary>
    /// Shared shape for the small, active-only definition tables: read the active rows, order
    /// them the way the seeder intended, and project the id/name/code triple every drop-down needs.
    /// </summary>
    private async Task<ListResultDto<LookupDto>> DefinitionsAsync<TEntity>(
        IReadOnlyRepository<TEntity> repository,
        Func<TEntity, string> name,
        Func<TEntity, string?> code,
        Func<TEntity, int> sortOrder,
        CancellationToken cancellationToken)
        where TEntity : class, IEntity<int>, IActivatable
    {
        await CheckPermissionAsync(EnsaPermissions.Lookups.Default);

        var records = await repository.GetListAsync(entity => entity.IsActive, cancellationToken);

        var result = records
            .OrderBy(sortOrder)
            .ThenBy(name, StringComparer.CurrentCulture)
            .Select(entity => new LookupDto
            {
                Id = entity.Id,
                DisplayName = name(entity),
                Code = code(entity),
                IsActive = entity.IsActive
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }
    /// <inheritdoc />
    public Task<ListResultDto<LookupDto>> GetPaymentMethodsAsync(
        CancellationToken cancellationToken = default)
        // PaymentMethod carries no code or sort order; the name is all a picker needs.
        => DefinitionsAsync(paymentMethodRepository, m => m.Name, _ => null, _ => 0, cancellationToken);

    /// <inheritdoc />
    public Task<ListResultDto<LookupDto>> GetServiceItemsAsync(
        CancellationToken cancellationToken = default)
        => DefinitionsAsync(serviceItemRepository, i => i.Name, i => i.Code, _ => 0, cancellationToken);

    /// <inheritdoc />
    public Task<ListResultDto<LookupDto>> GetMenuTypesAsync(
        CancellationToken cancellationToken = default)
        => DefinitionsAsync(menuTypeRepository, t => t.Name, t => t.Code, _ => 0, cancellationToken);
}
