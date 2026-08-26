using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Plans.Dtos;
using Ensa.Application.Contracts.Plans.Dtos.Navigations;

namespace Ensa.Application.Contracts.Plans;

/// <summary>
/// Activity / document / revision catalogue application service.
/// <para>
/// <b>TENANCY.</b> This is a mixed host/tenant catalogue: <c>TenantId == null</c> entries are
/// shared with every organisation. The visibility split is applied by the global query
/// filter in <c>EnsaDbContext</c>, so no method here adds a manual tenant predicate.
/// </para>
/// </summary>
public interface IActivityAppService : IApplicationService
{
    Task<ActivityDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Activity with its group, period, parent and direct children.</summary>
    Task<ActivityNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ActivityListDto>> GetListAsync(
        GetActivityListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Lightweight records for drop-down lists.</summary>
    Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activities marked as defaults, used when generating a work plan. Passing a
    /// <paramref name="tenantId"/> narrows the result to that organisation's own entries plus
    /// the shared host entries.
    /// </summary>
    Task<ListResultDto<ActivityListDto>> GetDefaultsAsync(
        int? tenantId = null,
        CancellationToken cancellationToken = default);

    Task<ActivityDto> CreateAsync(CreateActivityDto input, CancellationToken cancellationToken = default);

    Task<ActivityDto> UpdateAsync(int id, UpdateActivityDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
