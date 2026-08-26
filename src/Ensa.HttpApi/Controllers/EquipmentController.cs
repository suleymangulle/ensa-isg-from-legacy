using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Risks;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>Work equipment endpoints — <c>api/equipment</c>.</summary>
public class EquipmentController(IEquipmentAppService appService) : EnsaController
{
    /// <summary>Returns a single piece of equipment.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.Equipment.Default)]
    [ProducesResponseType<EquipmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EquipmentDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>Combined detail view: equipment, company, inspection report and documents.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.Equipment.Default)]
    [ProducesResponseType<EquipmentNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EquipmentNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => appService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable equipment list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Equipment.Default)]
    [ProducesResponseType<PagedResultDto<EquipmentListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<EquipmentListDto>> GetListAsync(
        [FromQuery] GetEquipmentListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);

    /// <summary>Equipment whose periodic inspection is missing or past due.</summary>
    [HttpGet("overdue-inspections")]
    [Authorize(EnsaPermissions.Equipment.Default)]
    [ProducesResponseType<ListResultDto<EquipmentListDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<EquipmentListDto>> GetOverdueInspectionsAsync(
        [FromQuery] int? companyId,
        CancellationToken cancellationToken)
        => appService.GetOverdueInspectionsAsync(companyId, cancellationToken);

    /// <summary>Creates a new equipment record.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.Equipment.Create)]
    [ProducesResponseType<EquipmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<EquipmentDto> CreateAsync(
        [FromBody] CreateEquipmentDto input,
        CancellationToken cancellationToken)
        => appService.CreateAsync(input, cancellationToken);

    /// <summary>Updates an existing equipment record.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.Equipment.Update)]
    [ProducesResponseType<EquipmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EquipmentDto> UpdateAsync(
        int id,
        [FromBody] UpdateEquipmentDto input,
        CancellationToken cancellationToken)
        => appService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the equipment; refused when the record is flagged as not deletable.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.Equipment.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => appService.DeleteAsync(id, cancellationToken);

    // --------------------------------------------------------------- Documents

    /// <summary>Documents attached to the equipment.</summary>
    [HttpGet("{id:int}/documents")]
    [Authorize(EnsaPermissions.Equipment.Default)]
    [ProducesResponseType<ListResultDto<EquipmentDocumentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<EquipmentDocumentDto>> GetDocumentsAsync(int id, CancellationToken cancellationToken)
        => appService.GetDocumentsAsync(id, cancellationToken);

    /// <summary>Attaches a document to the equipment.</summary>
    [HttpPost("{id:int}/documents")]
    [Authorize(EnsaPermissions.Equipment.Update)]
    [ProducesResponseType<EquipmentDocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<EquipmentDocumentDto> AddDocumentAsync(
        int id,
        [FromBody] CreateEquipmentDocumentDto input,
        CancellationToken cancellationToken)
        => appService.AddDocumentAsync(id, input, cancellationToken);

    /// <summary>Detaches a document from the equipment.</summary>
    [HttpDelete("{id:int}/documents/{equipmentDocumentId:int}")]
    [Authorize(EnsaPermissions.Equipment.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RemoveDocumentAsync(int id, int equipmentDocumentId, CancellationToken cancellationToken)
        => appService.RemoveDocumentAsync(id, equipmentDocumentId, cancellationToken);
}
