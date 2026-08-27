using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Documents;
using Ensa.Application.Contracts.Documents.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Form and template endpoints - <c>api/form</c>.
/// <para>
/// A form record points at a row in the central document store; the file itself is transferred
/// through the storage layer described on <c>api/document</c>.
/// </para>
/// </summary>
public class FormController(IFormAppService formAppService) : EnsaController
{
    /// <summary>Returns a single form definition.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<FormDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<FormDto> GetAsync(int id, CancellationToken cancellationToken)
        => formAppService.GetAsync(id, cancellationToken);

    /// <summary>Paged, filterable form list.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<FormListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<FormListDto>> GetListAsync(
        [FromQuery] GetFormListInput input,
        CancellationToken cancellationToken)
        => formAppService.GetListAsync(input, cancellationToken);

    /// <summary>Lightweight records for drop-downs (at most 50).</summary>
    [HttpGet("lookup")]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetLookupAsync(
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
        => formAppService.GetLookupAsync(filter, cancellationToken);

    /// <summary>Creates a form definition.</summary>
    [HttpPost]
    [ProducesResponseType<FormDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<FormDto> CreateAsync(
        [FromBody] CreateFormDto input,
        CancellationToken cancellationToken)
        => formAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates the form definition.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<FormDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<FormDto> UpdateAsync(
        int id,
        [FromBody] UpdateFormDto input,
        CancellationToken cancellationToken)
        => formAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the form definition.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => formAppService.DeleteAsync(id, cancellationToken);
}
