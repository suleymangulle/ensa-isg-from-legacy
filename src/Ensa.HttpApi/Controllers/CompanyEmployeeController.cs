using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Companies;
using Ensa.Application.Contracts.Companies.Dtos;
using Ensa.Application.Contracts.Companies.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Company employee endpoints — <c>api/company-employee</c>.
/// <para>
/// Authorization is enforced by policy; error shaping is done by
/// <c>EnsaExceptionFilter</c>, so there is no <c>try/catch</c> here.
/// </para>
/// </summary>
public class CompanyEmployeeController(ICompanyEmployeeAppService companyEmployeeAppService) : EnsaController
{
    /// <summary>Returns a single employee record.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.CompanyEmployee.Default)]
    [ProducesResponseType<CompanyEmployeeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CompanyEmployeeDto> GetAsync(int id, CancellationToken cancellationToken)
        => companyEmployeeAppService.GetAsync(id, cancellationToken);

    /// <summary>
    /// Combined view for the detail screen (workplace, department, health records,
    /// immunizations, family history, work history, duties and latest trainings).
    /// </summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.CompanyEmployee.Default)]
    [ProducesResponseType<CompanyEmployeeNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CompanyEmployeeNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => companyEmployeeAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable employee list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.CompanyEmployee.Default)]
    [ProducesResponseType<PagedResultDto<CompanyEmployeeListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<CompanyEmployeeListDto>> GetListAsync(
        [FromQuery] GetCompanyEmployeeListInput input,
        CancellationToken cancellationToken)
        => companyEmployeeAppService.GetListAsync(input, cancellationToken);

    /// <summary>Lightweight records for drop-down lists (at most 50).</summary>
    [HttpGet("lookup")]
    [Authorize(EnsaPermissions.CompanyEmployee.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetLookupAsync(
        [FromQuery] int? companyId,
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
        => companyEmployeeAppService.GetLookupAsync(companyId, filter, cancellationToken);

    /// <summary>Creates a new employee.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.CompanyEmployee.Create)]
    [ProducesResponseType<CompanyEmployeeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<CompanyEmployeeDto> CreateAsync(
        [FromBody] CreateCompanyEmployeeDto input,
        CancellationToken cancellationToken)
        => companyEmployeeAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates an existing employee.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.CompanyEmployee.Update)]
    [ProducesResponseType<CompanyEmployeeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CompanyEmployeeDto> UpdateAsync(
        int id,
        [FromBody] UpdateCompanyEmployeeDto input,
        CancellationToken cancellationToken)
        => companyEmployeeAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the employee (soft delete).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.CompanyEmployee.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => companyEmployeeAppService.DeleteAsync(id, cancellationToken);

    /// <summary>Terminates the employee and stores the exit date.</summary>
    [HttpPost("{id:int}/terminate")]
    [Authorize(EnsaPermissions.CompanyEmployee.Update)]
    [ProducesResponseType<CompanyEmployeeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CompanyEmployeeDto> TerminateAsync(
        int id,
        [FromBody] TerminateCompanyEmployeeDto input,
        CancellationToken cancellationToken)
        => companyEmployeeAppService.TerminateAsync(id, input.ExitDate, cancellationToken);

    /// <summary>Brings a terminated employee back into active service.</summary>
    [HttpPost("{id:int}/reinstate")]
    [Authorize(EnsaPermissions.CompanyEmployee.Update)]
    [ProducesResponseType<CompanyEmployeeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CompanyEmployeeDto> ReinstateAsync(int id, CancellationToken cancellationToken)
        => companyEmployeeAppService.ReinstateAsync(id, cancellationToken);
}
