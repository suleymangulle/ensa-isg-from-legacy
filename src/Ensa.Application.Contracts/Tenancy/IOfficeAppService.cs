using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Tenancy.Dtos;
using Ensa.Application.Contracts.Tenancy.Dtos.Navigations;

namespace Ensa.Application.Contracts.Tenancy;

/// <summary>Application service for the physical offices/branches of an organization.</summary>
public interface IOfficeAppService : IApplicationService
{
    Task<OfficeDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Combined view for the detail screen (organization, location, counters).</summary>
    Task<OfficeNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<OfficeListDto>> GetListAsync(
        GetOfficeListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Lightweight records for drop-down lists.</summary>
    Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>Refused when the organization already has a headquarters office.</summary>
    Task<OfficeDto> CreateAsync(CreateOfficeDto input, CancellationToken cancellationToken = default);

    /// <summary>Refused when another office already is the organization's headquarters.</summary>
    Task<OfficeDto> UpdateAsync(
        int id,
        UpdateOfficeDto input,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
