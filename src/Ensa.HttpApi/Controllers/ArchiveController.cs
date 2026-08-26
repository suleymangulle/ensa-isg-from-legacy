using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Documents;
using Ensa.Application.Contracts.Documents.Dtos;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Module archive endpoints - <c>api/archive</c>.
/// <para>
/// An archive row is a filing card for a stored document, so it is guarded by the document
/// permissions rather than by a set of its own.
/// </para>
/// </summary>
public class ArchiveController(IArchiveAppService archiveAppService) : EnsaController
{
    /// <summary>Returns a single archive entry.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.Document.Default)]
    [ProducesResponseType<ArchiveDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ArchiveDto> GetAsync(int id, CancellationToken cancellationToken)
        => archiveAppService.GetAsync(id, cancellationToken);

    /// <summary>Paged, filterable archive list.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Document.Default)]
    [ProducesResponseType<PagedResultDto<ArchiveListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<ArchiveListDto>> GetListAsync(
        [FromQuery] GetArchiveListInput input,
        CancellationToken cancellationToken)
        => archiveAppService.GetListAsync(input, cancellationToken);

    /// <summary>Archive entries of one module record, optionally narrowed to a period.</summary>
    [HttpGet("by-module/{moduleType}/{moduleId:int}")]
    [Authorize(EnsaPermissions.Document.Default)]
    [ProducesResponseType<ListResultDto<ArchiveListDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<ArchiveListDto>> GetByModuleAsync(
        DocumentOwnerType moduleType,
        int moduleId,
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
        => archiveAppService.GetByModuleAsync(moduleType, moduleId, month, year, cancellationToken);

    /// <summary>Creates an archive entry.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.Document.Create)]
    [ProducesResponseType<ArchiveDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ArchiveDto> CreateAsync(
        [FromBody] CreateArchiveDto input,
        CancellationToken cancellationToken)
        => archiveAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates the archive entry.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.Document.Update)]
    [ProducesResponseType<ArchiveDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ArchiveDto> UpdateAsync(
        int id,
        [FromBody] UpdateArchiveDto input,
        CancellationToken cancellationToken)
        => archiveAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the archive entry (soft delete).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.Document.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => archiveAppService.DeleteAsync(id, cancellationToken);
}
