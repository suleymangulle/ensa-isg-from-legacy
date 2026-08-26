using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;

namespace Ensa.Application.Contracts.Risks;

/// <summary>Corrective / preventive action (DOF) application service.</summary>
public interface ICorrectiveActionAppService : IApplicationService
{
    Task<CorrectiveActionDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Detail projection: action, company, owner, documents and source observation line.</summary>
    Task<CorrectiveActionNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<CorrectiveActionListDto>> GetListAsync(
        GetCorrectiveActionListInput input,
        CancellationToken cancellationToken = default);

    Task<CorrectiveActionDto> CreateAsync(
        CreateCorrectiveActionDto input,
        CancellationToken cancellationToken = default);

    Task<CorrectiveActionDto> UpdateAsync(
        int id,
        UpdateCorrectiveActionDto input,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Dashboard indicator: number of actions still in progress for a company.</summary>
    Task<int> GetOpenCountAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>Open actions whose deadline has already passed.</summary>
    Task<ListResultDto<CorrectiveActionListDto>> GetOverdueAsync(
        int? companyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Closes an open action with its result text and result date.</summary>
    Task<CorrectiveActionDto> CloseAsync(
        int id,
        string result,
        DateTime resultDate,
        CancellationToken cancellationToken = default);
}
