using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Health;
using Ensa.Application.Contracts.Health.Dtos;
using Ensa.Application.Contracts.Health.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// E-prescription endpoints — <c>api/eprescription</c>.
/// <para>
/// <b>PRIVACY.</b> Medication and ICD-10 diagnosis lines are health data and are served only
/// by <c>{id}/detail</c>, one prescription at a time. The list endpoint returns the
/// prescription envelope alone.
/// </para>
/// </summary>
public class EPrescriptionController(IEPrescriptionAppService appService) : EnsaController
{
    /// <summary>Returns one prescription header.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.EPrescription.Default)]
    [ProducesResponseType<EPrescriptionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EPrescriptionDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>Prescription with the patient, medication lines and diagnosis lines.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.EPrescription.Default)]
    [ProducesResponseType<EPrescriptionNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EPrescriptionNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => appService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable list of prescriptions.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.EPrescription.Default)]
    [ProducesResponseType<PagedResultDto<EPrescriptionListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<EPrescriptionListDto>> GetListAsync(
        [FromQuery] GetEPrescriptionListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);

    /// <summary>Creates a prescription together with its medication and diagnosis lines.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.EPrescription.Create)]
    [ProducesResponseType<EPrescriptionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<EPrescriptionDto> CreateAsync(
        [FromBody] CreateEPrescriptionDto input,
        CancellationToken cancellationToken)
        => appService.CreateAsync(input, cancellationToken);

    /// <summary>Updates the header and replaces both line sets.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.EPrescription.Update)]
    [ProducesResponseType<EPrescriptionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EPrescriptionDto> UpdateAsync(
        int id,
        [FromBody] UpdateEPrescriptionDto input,
        CancellationToken cancellationToken)
        => appService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes a prescription that has not been submitted yet.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.EPrescription.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => appService.DeleteAsync(id, cancellationToken);

    /// <summary>Cancels a prescription, recording the reason.</summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(EnsaPermissions.EPrescription.Update)]
    [ProducesResponseType<EPrescriptionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EPrescriptionDto> CancelAsync(
        int id,
        [FromBody] CancelEPrescriptionRequest input,
        CancellationToken cancellationToken)
        => appService.CancelAsync(id, input.Reason, cancellationToken);
}

/// <summary>Body of the prescription cancellation request.</summary>
/// <param name="Reason">Why the prescription is being cancelled.</param>
public sealed record CancelEPrescriptionRequest(string Reason);
