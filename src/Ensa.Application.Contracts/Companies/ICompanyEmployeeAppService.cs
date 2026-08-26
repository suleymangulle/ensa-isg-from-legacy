using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Companies.Dtos;
using Ensa.Application.Contracts.Companies.Dtos.Navigations;

namespace Ensa.Application.Contracts.Companies;

/// <summary>Application service for the employees of a served workplace.</summary>
public interface ICompanyEmployeeAppService : IApplicationService
{
    Task<CompanyEmployeeDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Combined view for the detail screen (workplace, department, health records,
    /// immunizations, family history, work history, duties and latest trainings).
    /// </summary>
    Task<CompanyEmployeeNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<CompanyEmployeeListDto>> GetListAsync(
        GetCompanyEmployeeListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Lightweight records for drop-down lists.</summary>
    Task<ListResultDto<LookupDto>> GetLookupAsync(
        int? companyId = null,
        string? filter = null,
        CancellationToken cancellationToken = default);

    Task<CompanyEmployeeDto> CreateAsync(
        CreateCompanyEmployeeDto input,
        CancellationToken cancellationToken = default);

    Task<CompanyEmployeeDto> UpdateAsync(
        int id,
        UpdateCompanyEmployeeDto input,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Terminates the employee (deactivates the record and stores the exit date).</summary>
    Task<CompanyEmployeeDto> TerminateAsync(
        int id,
        DateTime exitDate,
        CancellationToken cancellationToken = default);

    /// <summary>Brings a terminated employee back into active service.</summary>
    Task<CompanyEmployeeDto> ReinstateAsync(int id, CancellationToken cancellationToken = default);
}
