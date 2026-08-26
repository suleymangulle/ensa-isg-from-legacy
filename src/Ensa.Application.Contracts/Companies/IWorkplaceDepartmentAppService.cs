using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Companies.Dtos;
using Ensa.Application.Contracts.Companies.Dtos.Navigations;

namespace Ensa.Application.Contracts.Companies;

/// <summary>Application service for the physical/organizational departments of a workplace.</summary>
public interface IWorkplaceDepartmentAppService : IApplicationService
{
    Task<WorkplaceDepartmentDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Combined view for the detail screen (workplace, documents, employee count).</summary>
    Task<WorkplaceDepartmentNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<WorkplaceDepartmentListDto>> GetListAsync(
        GetWorkplaceDepartmentListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Departments of one workplace, for drop-down lists.</summary>
    Task<ListResultDto<LookupDto>> GetLookupAsync(
        int companyId,
        CancellationToken cancellationToken = default);

    Task<WorkplaceDepartmentDto> CreateAsync(
        CreateWorkplaceDepartmentDto input,
        CancellationToken cancellationToken = default);

    Task<WorkplaceDepartmentDto> UpdateAsync(
        int id,
        UpdateWorkplaceDepartmentDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Refused while employees are still assigned to the department.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
