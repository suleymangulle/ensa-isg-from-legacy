using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Risks;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>Corrective / preventive action (DOF) endpoints — <c>api/corrective-action</c>.</summary>
public class CorrectiveActionController(ICorrectiveActionAppService appService) : EnsaController
{
    /// <summary>Returns a single corrective action.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<CorrectiveActionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CorrectiveActionDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>Combined detail view: action, company, owner, documents and source line.</summary>
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType<CorrectiveActionNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CorrectiveActionNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => appService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable corrective action list.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<CorrectiveActionListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<CorrectiveActionListDto>> GetListAsync(
        [FromQuery] GetCorrectiveActionListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);

    /// <summary>Dashboard indicator: number of actions still in progress for a company.</summary>
    [HttpGet("open-count/{companyId:int}")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public Task<int> GetOpenCountAsync(int companyId, CancellationToken cancellationToken)
        => appService.GetOpenCountAsync(companyId, cancellationToken);

    /// <summary>Open actions whose deadline has already passed.</summary>
    [HttpGet("overdue")]
    [ProducesResponseType<ListResultDto<CorrectiveActionListDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<CorrectiveActionListDto>> GetOverdueAsync(
        [FromQuery] int? companyId,
        CancellationToken cancellationToken)
        => appService.GetOverdueAsync(companyId, cancellationToken);

    /// <summary>Creates a new corrective action.</summary>
    [HttpPost]
    [ProducesResponseType<CorrectiveActionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<CorrectiveActionDto> CreateAsync(
        [FromBody] CreateCorrectiveActionDto input,
        CancellationToken cancellationToken)
        => appService.CreateAsync(input, cancellationToken);

    /// <summary>Updates an existing corrective action (closing data is not touched).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<CorrectiveActionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CorrectiveActionDto> UpdateAsync(
        int id,
        [FromBody] UpdateCorrectiveActionDto input,
        CancellationToken cancellationToken)
        => appService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Closes an open corrective action with its result and result date.</summary>
    [HttpPost("{id:int}/close")]
    [ProducesResponseType<CorrectiveActionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<CorrectiveActionDto> CloseAsync(
        int id,
        [FromBody] CloseCorrectiveActionDto input,
        CancellationToken cancellationToken)
        => appService.CloseAsync(id, input.Result, input.ResultDate, cancellationToken);

    /// <summary>Deletes the corrective action (soft delete).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => appService.DeleteAsync(id, cancellationToken);
}
