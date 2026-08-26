using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Trainings;
using Ensa.Application.Contracts.Trainings.Dtos;
using Ensa.Application.Contracts.Trainings.Dtos.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Training catalogue endpoints — <c>api/training</c>.
/// <para>
/// Hazard-class durations travel as a list of <c>{ hazardClass, durationMinutes }</c> pairs,
/// both for the training and for each of its topics.
/// </para>
/// </summary>
public class TrainingController(ITrainingAppService appService) : EnsaController
{
    /// <summary>Returns one training with its hazard-class durations.</summary>
    [HttpGet("{id:int}")]
    [Authorize(EnsaPermissions.Training.Default)]
    [ProducesResponseType<TrainingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>Training with its group, durations, topics and exams.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(EnsaPermissions.Training.Default)]
    [ProducesResponseType<TrainingNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => appService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable training catalogue.</summary>
    [HttpGet]
    [Authorize(EnsaPermissions.Training.Default)]
    [ProducesResponseType<PagedResultDto<TrainingListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<TrainingListDto>> GetListAsync(
        [FromQuery] GetTrainingListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);

    /// <summary>Lightweight records for drop-down lists.</summary>
    [HttpGet("lookup")]
    [Authorize(EnsaPermissions.Training.Default)]
    [ProducesResponseType<ListResultDto<LookupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<LookupDto>> GetLookupAsync(
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
        => appService.GetLookupAsync(filter, cancellationToken);

    /// <summary>Trainings marked as mandatory defaults.</summary>
    [HttpGet("defaults")]
    [Authorize(EnsaPermissions.Training.Default)]
    [ProducesResponseType<ListResultDto<TrainingListDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<TrainingListDto>> GetDefaultsAsync(
        [FromQuery] int? tenantId,
        CancellationToken cancellationToken)
        => appService.GetDefaultsAsync(tenantId, cancellationToken);

    /// <summary>Creates a training with its hazard-class durations.</summary>
    [HttpPost]
    [Authorize(EnsaPermissions.Training.Create)]
    [ProducesResponseType<TrainingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<TrainingDto> CreateAsync(
        [FromBody] CreateTrainingDto input,
        CancellationToken cancellationToken)
        => appService.CreateAsync(input, cancellationToken);

    /// <summary>Updates a training and replaces its duration set.</summary>
    [HttpPut("{id:int}")]
    [Authorize(EnsaPermissions.Training.Update)]
    [ProducesResponseType<TrainingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingDto> UpdateAsync(
        int id,
        [FromBody] UpdateTrainingDto input,
        CancellationToken cancellationToken)
        => appService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes a training with its topics and durations.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(EnsaPermissions.Training.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => appService.DeleteAsync(id, cancellationToken);

    // ------------------------------------------------------------------ Topics

    /// <summary>Topics of a training in display order, each with its durations.</summary>
    [HttpGet("{id:int}/topics")]
    [Authorize(EnsaPermissions.Training.Default)]
    [ProducesResponseType<ListResultDto<TrainingTopicDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<TrainingTopicDto>> GetTopicsAsync(int id, CancellationToken cancellationToken)
        => appService.GetTopicsAsync(id, cancellationToken);

    /// <summary>Adds a topic with its durations.</summary>
    [HttpPost("{id:int}/topics")]
    [Authorize(EnsaPermissions.Training.Create)]
    [ProducesResponseType<TrainingTopicDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingTopicDto> CreateTopicAsync(
        int id,
        [FromBody] CreateTrainingTopicDto input,
        CancellationToken cancellationToken)
        => appService.CreateTopicAsync(id, input, cancellationToken);

    /// <summary>Updates a topic and replaces its duration set.</summary>
    [HttpPut("{id:int}/topics/{topicId:int}")]
    [Authorize(EnsaPermissions.Training.Update)]
    [ProducesResponseType<TrainingTopicDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingTopicDto> UpdateTopicAsync(
        int id,
        int topicId,
        [FromBody] UpdateTrainingTopicDto input,
        CancellationToken cancellationToken)
        => appService.UpdateTopicAsync(id, topicId, input, cancellationToken);

    /// <summary>Removes a topic and its durations.</summary>
    [HttpDelete("{id:int}/topics/{topicId:int}")]
    [Authorize(EnsaPermissions.Training.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteTopicAsync(int id, int topicId, CancellationToken cancellationToken)
        => appService.DeleteTopicAsync(id, topicId, cancellationToken);

    // ------------------------------------------------- Statutory calculations

    /// <summary>
    /// Whether an employee's training is still within its statutory refresh interval,
    /// together with the mandatory duration for the hazard class.
    /// </summary>
    [HttpGet("{id:int}/validity")]
    [Authorize(EnsaPermissions.Training.Default)]
    [ProducesResponseType<TrainingValidityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TrainingValidityDto> GetValidityAsync(
        int id,
        [FromQuery] int companyEmployeeId,
        [FromQuery] HazardClass hazardClass,
        CancellationToken cancellationToken)
        => appService.GetValidityAsync(companyEmployeeId, id, hazardClass, cancellationToken);
}
