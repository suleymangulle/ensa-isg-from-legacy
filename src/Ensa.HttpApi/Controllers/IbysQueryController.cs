using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Ibys;
using Ensa.Application.Contracts.Ibys.Dtos;
using Ensa.Application.Contracts.Ibys.Dtos.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// IBYS submission tracking endpoints — <c>api/ibys-query</c>.
/// <para>
/// <b>SECURITY.</b> No endpoint here returns the notification XML, the e-signed payload or
/// the e-signature licence key. The XML is an encrypted payload carrying clinical data, the
/// signed blob is a reusable signed artefact and the licence key is a secret; all three stay
/// inside the domain and are read only by the background submission worker.
/// </para>
/// </summary>
public class IbysQueryController(IIbysQueryAppService appService) : EnsaController
{
    /// <summary>Returns one submission record.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<IbysQueryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IbysQueryDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>Submission with the workplace, the employee and the attached forms.</summary>
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType<IbysQueryNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IbysQueryNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => appService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable list of submissions.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<IbysQueryListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<IbysQueryListDto>> GetListAsync(
        [FromQuery] GetIbysQueryListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);

    /// <summary>Submissions of the given type still awaiting an IBYS result.</summary>
    [HttpGet("pending")]
    [ProducesResponseType<ListResultDto<IbysQueryListDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<IbysQueryListDto>> GetPendingAsync(
        [FromQuery] IbysQueryType type,
        [FromQuery] int maxResultCount,
        CancellationToken cancellationToken)
        => appService.GetPendingAsync(type, maxResultCount, cancellationToken);

    /// <summary>
    /// Moves the submission to a new status. The transition is validated by
    /// <c>IIbysSubmissionManager</c>.
    /// </summary>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType<IbysQueryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IbysQueryDto> UpdateStatusAsync(
        int id,
        [FromBody] UpdateIbysQueryStatusDto input,
        CancellationToken cancellationToken)
        => appService.UpdateStatusAsync(id, input.Status, input.Message, input.SubmissionNumber, cancellationToken);
}
