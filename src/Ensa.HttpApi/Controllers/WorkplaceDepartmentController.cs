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
/// Workplace department endpoints — <c>api/workplace-department</c>.
/// <para>
/// Authorization is enforced by policy; error shaping is done by
/// <c>EnsaExceptionFilter</c>, so there is no <c>try/catch</c> here.
/// </para>
/// </summary>
public class WorkplaceDepartmentController(IWorkplaceDepartmentAppService workplaceDepartmentAppService)
    : EnsaController
{
    /// <summary>Returns a single department record.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.WorkplaceDepartment.Default)]
    [ProducesResponseType<WorkplaceDepartmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkplaceDepartmentDto> GetAsync(int id, CancellationToken cancellationToken)
        => workplaceDepartmentAppService.GetAsync(id, cancellationToken);

    /// <summary>Combined view for the detail screen (workplace, documents, employee count).</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.WorkplaceDepartment.Default)]
    [ProducesResponseType<WorkplaceDepartmentNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkplaceDepartmentNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken)
        => workplaceDepartmentAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable department list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.WorkplaceDepartment.Default)]
    [ProducesResponseType<PagedResultDto<WorkplaceDepartmentListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<WorkplaceDepartmentListDto>> GetListAsync(
        [FromQuery] GetWorkplaceDepartmentListInput input,
        CancellationToken cancellationToken)
        => workplaceDepartmentAppService.GetListAsync(input, cancellationToken);

    /// <summary>Departments of one workplace, for drop-down lists.</summary>
    [HttpGet("lookup")]
    [Authorize(EnsaPermissions.WorkplaceDepartment.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetLookupAsync(
        [FromQuery] int companyId,
        CancellationToken cancellationToken)
        => workplaceDepartmentAppService.GetLookupAsync(companyId, cancellationToken);

    /// <summary>Creates a new department.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.WorkplaceDepartment.Create)]
    [ProducesResponseType<WorkplaceDepartmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<WorkplaceDepartmentDto> CreateAsync(
        [FromBody] CreateWorkplaceDepartmentDto input,
        CancellationToken cancellationToken)
        => workplaceDepartmentAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates an existing department.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.WorkplaceDepartment.Update)]
    [ProducesResponseType<WorkplaceDepartmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkplaceDepartmentDto> UpdateAsync(
        int id,
        [FromBody] UpdateWorkplaceDepartmentDto input,
        CancellationToken cancellationToken)
        => workplaceDepartmentAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the department; refused while employees are still assigned to it.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.WorkplaceDepartment.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => workplaceDepartmentAppService.DeleteAsync(id, cancellationToken);
}
