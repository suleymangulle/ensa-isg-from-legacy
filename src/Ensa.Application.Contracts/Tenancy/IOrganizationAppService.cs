using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Tenancy.Dtos;
using Ensa.Application.Contracts.Tenancy.Dtos.Navigations;

namespace Ensa.Application.Contracts.Tenancy;

/// <summary>
/// Application service for organizations (tenants).
/// <para>
/// <c>Organization</c> is a host entity, so this service is meant for system
/// administrators; every method is guarded by <c>EnsaPermissions.Tenant.*</c>.
/// </para>
/// </summary>
public interface IOrganizationAppService : IApplicationService
{
    Task<OrganizationDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Combined view for the detail screen (type, plan, location, offices, counters).</summary>
    Task<OrganizationNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<OrganizationListDto>> GetListAsync(
        GetOrganizationListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Lightweight records for drop-down lists.</summary>
    Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default);

    Task<OrganizationDto> CreateAsync(
        CreateOrganizationDto input,
        CancellationToken cancellationToken = default);

    Task<OrganizationDto> UpdateAsync(
        int id,
        UpdateOrganizationDto input,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
