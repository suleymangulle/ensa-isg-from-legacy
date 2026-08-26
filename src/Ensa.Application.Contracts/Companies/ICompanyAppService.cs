using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Companies.Dtos;
using Ensa.Application.Contracts.Companies.Dtos.Navigations;

namespace Ensa.Application.Contracts.Companies;

/// <summary>Application service for companies (the workplaces being served).</summary>
public interface ICompanyAppService : IApplicationService
{
    Task<CompanyDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Combined view for the detail screen (city, branches, assigned specialists, departments).</summary>
    Task<CompanyNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<CompanyListDto>> GetListAsync(
        GetCompanyListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Lightweight records for lookup lists.</summary>
    Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default);

    Task<CompanyDto> CreateAsync(CreateCompanyDto input, CancellationToken cancellationToken = default);

    Task<CompanyDto> UpdateAsync(int id, UpdateCompanyDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
