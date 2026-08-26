using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Risks;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Work accident / near miss / occupational disease endpoints — <c>api/incident</c>.
/// <para>SSI notification figures on the responses are produced by <c>IIncidentManager</c>.</para>
/// </summary>
public class IncidentController(IIncidentAppService appService) : EnsaController
{
    /// <summary>Returns a single incident.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.Incident.Default)]
    [ProducesResponseType<IncidentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IncidentDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>Combined detail view: incident, department, document and person lists.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.Incident.Default)]
    [ProducesResponseType<IncidentNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IncidentNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => appService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable incident list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Incident.Default)]
    [ProducesResponseType<PagedResultDto<IncidentListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<IncidentListDto>> GetListAsync(
        [FromQuery] GetIncidentListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);

    /// <summary>Creates a new incident record.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.Incident.Create)]
    [ProducesResponseType<IncidentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IncidentDto> CreateAsync(
        [FromBody] CreateIncidentDto input,
        CancellationToken cancellationToken)
        => appService.CreateAsync(input, cancellationToken);

    /// <summary>Updates an existing incident record.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.Incident.Update)]
    [ProducesResponseType<IncidentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IncidentDto> UpdateAsync(
        int id,
        [FromBody] UpdateIncidentDto input,
        CancellationToken cancellationToken)
        => appService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the incident together with its person records (soft delete).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.Incident.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => appService.DeleteAsync(id, cancellationToken);

    // ----------------------------------------------------------------- Persons

    /// <summary>People involved in the incident, optionally filtered by role.</summary>
    [HttpGet("{id:int}/persons")]
    [Authorize(EnsaPermissions.Incident.Default)]
    [ProducesResponseType<ListResultDto<IncidentPersonDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<IncidentPersonDto>> GetPersonsAsync(
        int id,
        [FromQuery] IncidentPersonRole? personType,
        CancellationToken cancellationToken)
        => appService.GetPersonsAsync(id, personType, cancellationToken);

    /// <summary>Adds an affected / witness / responder person to the incident.</summary>
    [HttpPost("{id:int}/persons")]
    [Authorize(EnsaPermissions.Incident.Update)]
    [ProducesResponseType<IncidentPersonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IncidentPersonDto> AddPersonAsync(
        int id,
        [FromBody] CreateIncidentPersonDto input,
        CancellationToken cancellationToken)
        => appService.AddPersonAsync(id, input, cancellationToken);

    /// <summary>Removes a person from the incident.</summary>
    [HttpDelete("{id:int}/persons/{personId:int}")]
    [Authorize(EnsaPermissions.Incident.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RemovePersonAsync(int id, int personId, CancellationToken cancellationToken)
        => appService.RemovePersonAsync(id, personId, cancellationToken);

    // --------------------------------------------------------------- Analytics

    /// <summary>Total lost work days in a period — input of the accident frequency / severity rate.</summary>
    [HttpGet("lost-work-days/{companyId:int}")]
    [Authorize(EnsaPermissions.Incident.Default)]
    [ProducesResponseType<LostWorkDaysSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<LostWorkDaysSummaryDto> GetTotalLostWorkDaysAsync(
        int companyId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
        => appService.GetTotalLostWorkDaysAsync(companyId, from, to, cancellationToken);
}
