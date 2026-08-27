using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Companies;
using Ensa.Application.Contracts.Companies.Dtos;
using Ensa.Application.Contracts.Companies.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Company (the serviced workplace) endpoints — <c>api/company</c>.
/// <para>
/// The <b>reference controller</b> for the other modules: authorization goes through
/// policies and errors through <c>EnsaExceptionFilter</c>, so there is no <c>try/catch</c> here.
/// </para>
/// </summary>
public class CompanyController(ICompanyAppService companyAppService) : EnsaController
{
    /// <summary>Returns a single company record.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<CompanyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CompanyDto> GetAsync(int id, CancellationToken cancellationToken)
        => companyAppService.GetAsync(id, cancellationToken);

    /// <summary>Composite view for the detail screen (city, branches, assigned specialists, departments).</summary>
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType<CompanyNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CompanyNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => companyAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable list of companies.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<CompanyListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<CompanyListDto>> GetListAsync(
        [FromQuery] GetCompanyListInput input,
        CancellationToken cancellationToken)
        => companyAppService.GetListAsync(input, cancellationToken);

    /// <summary>Lightweight records for drop-down lists (50 at most).</summary>
    [HttpGet("lookup")]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetLookupAsync(
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
        => companyAppService.GetLookupAsync(filter, cancellationToken);

    /// <summary>Creates a new company.</summary>
    [HttpPost]
    [ProducesResponseType<CompanyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<CompanyDto> CreateAsync(
        [FromBody] CreateCompanyDto input,
        CancellationToken cancellationToken)
        => companyAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates an existing company.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<CompanyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CompanyDto> UpdateAsync(
        int id,
        [FromBody] UpdateCompanyDto input,
        CancellationToken cancellationToken)
        => companyAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the company (soft delete).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => companyAppService.DeleteAsync(id, cancellationToken);
}
