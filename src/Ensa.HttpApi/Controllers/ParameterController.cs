using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Lookups;
using Ensa.Application.Contracts.Lookups.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Per-organization system setting endpoints - <c>api/parameter</c>.
/// <para>
/// Guarded by the reference-module permissions (<c>Ensa.Lookups</c>): parameters are the
/// writable half of the definitions area, and the tenant filter keeps organizations apart.
/// </para>
/// </summary>
public class ParameterController(IParameterAppService parameterAppService) : EnsaController
{
    /// <summary>Returns a single parameter.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ParameterDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ParameterDto> GetAsync(int id, CancellationToken cancellationToken)
        => parameterAppService.GetAsync(id, cancellationToken);

    /// <summary>Paged, filterable parameter list.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<ParameterListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<ParameterListDto>> GetListAsync(
        [FromQuery] GetParameterListInput input,
        CancellationToken cancellationToken)
        => parameterAppService.GetListAsync(input, cancellationToken);

    /// <summary>
    /// Reads one parameter value by code. Responds with <c>Exists = false</c> instead of 404
    /// when the code is not defined, so callers can fall back to a default.
    /// </summary>
    [HttpGet("value/{code}")]
    [ProducesResponseType<ParameterValueDto>(StatusCodes.Status200OK)]
    public Task<ParameterValueDto> GetValueAsync(string code, CancellationToken cancellationToken)
        => parameterAppService.GetValueAsync(code, cancellationToken);

    /// <summary>Creates a parameter.</summary>
    [HttpPost]
    [ProducesResponseType<ParameterDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ParameterDto> CreateAsync(
        [FromBody] CreateParameterDto input,
        CancellationToken cancellationToken)
        => parameterAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates the parameter. The code itself is immutable.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ParameterDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ParameterDto> UpdateAsync(
        int id,
        [FromBody] UpdateParameterDto input,
        CancellationToken cancellationToken)
        => parameterAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the parameter.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => parameterAppService.DeleteAsync(id, cancellationToken);
}
