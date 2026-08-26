using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Plans;
using Ensa.Application.Contracts.Plans.Dtos;
using Ensa.Application.Contracts.Plans.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Activity / document / revision catalogue endpoints — <c>api/activity</c>.
/// <para>
/// <b>TENANCY.</b> A mixed host/tenant catalogue: entries with no tenant are shared with
/// every organisation. The split is applied by the global query filter, so no endpoint takes
/// a tenant parameter for filtering.
/// </para>
/// </summary>
public class ActivityController(IActivityAppService appService) : EnsaController
{
    /// <summary>Returns one catalogue entry.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.Activity.Default)]
    [ProducesResponseType<ActivityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActivityDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>Activity with its group, period, parent and children.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.Activity.Default)]
    [ProducesResponseType<ActivityNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActivityNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => appService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable catalogue list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Activity.Default)]
    [ProducesResponseType<PagedResultDto<ActivityListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<ActivityListDto>> GetListAsync(
        [FromQuery] GetActivityListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);

    /// <summary>Lightweight records for drop-down lists.</summary>
    [HttpGet("lookup")]
    [Authorize(EnsaPermissions.Activity.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetLookupAsync(
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
        => appService.GetLookupAsync(filter, cancellationToken);

    /// <summary>Activities marked as defaults, used when generating a work plan.</summary>
    [HttpGet("defaults")]
    [Authorize(EnsaPermissions.Activity.Default)]
    [ProducesResponseType<ListResultDto<ActivityListDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<ActivityListDto>> GetDefaultsAsync(
        [FromQuery] int? tenantId,
        CancellationToken cancellationToken)
        => appService.GetDefaultsAsync(tenantId, cancellationToken);

    /// <summary>Creates a catalogue entry.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.Activity.Create)]
    [ProducesResponseType<ActivityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActivityDto> CreateAsync(
        [FromBody] CreateActivityDto input,
        CancellationToken cancellationToken)
        => appService.CreateAsync(input, cancellationToken);

    /// <summary>Updates a catalogue entry.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.Activity.Update)]
    [ProducesResponseType<ActivityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActivityDto> UpdateAsync(
        int id,
        [FromBody] UpdateActivityDto input,
        CancellationToken cancellationToken)
        => appService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes a catalogue entry that has no children.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.Activity.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => appService.DeleteAsync(id, cancellationToken);
}
