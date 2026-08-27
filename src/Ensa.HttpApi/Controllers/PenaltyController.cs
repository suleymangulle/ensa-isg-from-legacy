using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Finance;
using Ensa.Application.Contracts.Finance.Dtos;
using Ensa.Application.Contracts.Finance.Dtos.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Statutory fine endpoints — <c>api/penalty</c>.
/// <para>
/// Covers both the host fine catalogue and the tenant-scoped fine-risk surveys built on top of
/// it; the two concerns live in separate application services behind one route prefix.
/// </para>
/// </summary>
public class PenaltyController(
    IPenaltyAppService penaltyAppService,
    IPenaltySurveyAppService penaltySurveyAppService)
    : EnsaController
{
    /// <summary>Returns a single fine article.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PenaltyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PenaltyDto> GetAsync(int id, CancellationToken cancellationToken)
        => penaltyAppService.GetAsync(id, cancellationToken);

    /// <summary>The fine article together with its full amount matrix.</summary>
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType<PenaltyNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PenaltyNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken)
        => penaltyAppService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable fine catalogue.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<PenaltyListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<PenaltyListDto>> GetListAsync(
        [FromQuery] GetPenaltyListInput input,
        CancellationToken cancellationToken)
        => penaltyAppService.GetListAsync(input, cancellationToken);

    /// <summary>Creates a fine article.</summary>
    [HttpPost]
    [ProducesResponseType<PenaltyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<PenaltyDto> CreateAsync(
        [FromBody] CreatePenaltyDto input,
        CancellationToken cancellationToken)
        => penaltyAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates a fine article.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<PenaltyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PenaltyDto> UpdateAsync(
        int id,
        [FromBody] UpdatePenaltyDto input,
        CancellationToken cancellationToken)
        => penaltyAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes a fine article together with its amount matrix.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => penaltyAppService.DeleteAsync(id, cancellationToken);

    // --------------------------------------------------------- Amount matrix

    /// <summary>The amount matrix of one fine article.</summary>
    [HttpGet("{id:int}/amounts")]
    [ProducesResponseType<ListResultDto<PenaltyAmountDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ListResultDto<PenaltyAmountDto>> GetAmountsAsync(int id, CancellationToken cancellationToken)
        => penaltyAppService.GetAmountsAsync(id, cancellationToken);

    /// <summary>Adds a matrix cell.</summary>
    [HttpPost("{id:int}/amounts")]
    [ProducesResponseType<PenaltyAmountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<PenaltyAmountDto> AddAmountAsync(
        int id,
        [FromBody] CreatePenaltyAmountDto input,
        CancellationToken cancellationToken)
        => penaltyAppService.AddAmountAsync(id, input, cancellationToken);

    /// <summary>Updates a matrix cell.</summary>
    [HttpPut("{id:int}/amounts/{amountId:int}")]
    [ProducesResponseType<PenaltyAmountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PenaltyAmountDto> UpdateAmountAsync(
        int id,
        int amountId,
        [FromBody] UpdatePenaltyAmountDto input,
        CancellationToken cancellationToken)
        => penaltyAppService.UpdateAmountAsync(id, amountId, input, cancellationToken);

    /// <summary>Removes a matrix cell.</summary>
    [HttpDelete("{id:int}/amounts/{amountId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RemoveAmountAsync(int id, int amountId, CancellationToken cancellationToken)
        => penaltyAppService.RemoveAmountAsync(id, amountId, cancellationToken);

    /// <summary>The amount that applies to a workplace profile for a given year.</summary>
    [HttpGet("{id:int}/applicable-amount")]
    [ProducesResponseType<ApplicablePenaltyAmountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ApplicablePenaltyAmountDto> GetApplicableAmountAsync(
        int id,
        [FromQuery] HazardClass hazardClass,
        [FromQuery] EmployeeCountRange range,
        [FromQuery] int year,
        CancellationToken cancellationToken)
        => penaltyAppService.GetApplicableAmountAsync(id, hazardClass, range, year, cancellationToken);

    // -------------------------------------------------------------- Surveys

    /// <summary>Returns a single fine-risk survey.</summary>
    [HttpGet("surveys/{surveyId:int}")]
    [ProducesResponseType<PenaltySurveyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PenaltySurveyDto> GetSurveyAsync(int surveyId, CancellationToken cancellationToken)
        => penaltySurveyAppService.GetAsync(surveyId, cancellationToken);

    /// <summary>Paged, filterable survey list.</summary>
    [HttpGet("surveys")]
    [ProducesResponseType<PagedResultDto<PenaltySurveyListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<PenaltySurveyListDto>> GetSurveyListAsync(
        [FromQuery] GetPenaltySurveyListInput input,
        CancellationToken cancellationToken)
        => penaltySurveyAppService.GetListAsync(input, cancellationToken);

    /// <summary>Creates a fine-risk survey.</summary>
    [HttpPost("surveys")]
    [ProducesResponseType<PenaltySurveyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<PenaltySurveyDto> CreateSurveyAsync(
        [FromBody] CreatePenaltySurveyDto input,
        CancellationToken cancellationToken)
        => penaltySurveyAppService.CreateAsync(input, cancellationToken);

    /// <summary>Updates a fine-risk survey.</summary>
    [HttpPut("surveys/{surveyId:int}")]
    [ProducesResponseType<PenaltySurveyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PenaltySurveyDto> UpdateSurveyAsync(
        int surveyId,
        [FromBody] UpdatePenaltySurveyDto input,
        CancellationToken cancellationToken)
        => penaltySurveyAppService.UpdateAsync(surveyId, input, cancellationToken);

    /// <summary>Deletes a survey together with its answer lines.</summary>
    [HttpDelete("surveys/{surveyId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteSurveyAsync(int surveyId, CancellationToken cancellationToken)
        => penaltySurveyAppService.DeleteAsync(surveyId, cancellationToken);

    /// <summary>Paged answer lines of a survey.</summary>
    [HttpGet("surveys/lines")]
    [ProducesResponseType<PagedResultDto<PenaltySurveyLineDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PagedResultDto<PenaltySurveyLineDto>> GetSurveyLinesAsync(
        [FromQuery] GetPenaltySurveyLineListInput input,
        CancellationToken cancellationToken)
        => penaltySurveyAppService.GetLinesAsync(input, cancellationToken);

    /// <summary>Records an answer. The amount is resolved from the catalogue server-side.</summary>
    [HttpPost("surveys/{surveyId:int}/lines")]
    [ProducesResponseType<PenaltySurveyLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<PenaltySurveyLineDto> AddSurveyLineAsync(
        int surveyId,
        [FromBody] CreatePenaltySurveyLineDto input,
        CancellationToken cancellationToken)
        => penaltySurveyAppService.AddLineAsync(surveyId, input, cancellationToken);

    /// <summary>Updates an answer.</summary>
    [HttpPut("surveys/{surveyId:int}/lines/{lineId:int}")]
    [ProducesResponseType<PenaltySurveyLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PenaltySurveyLineDto> UpdateSurveyLineAsync(
        int surveyId,
        int lineId,
        [FromBody] UpdatePenaltySurveyLineDto input,
        CancellationToken cancellationToken)
        => penaltySurveyAppService.UpdateLineAsync(surveyId, lineId, input, cancellationToken);

    /// <summary>Removes an answer.</summary>
    [HttpDelete("surveys/{surveyId:int}/lines/{lineId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RemoveSurveyLineAsync(int surveyId, int lineId, CancellationToken cancellationToken)
        => penaltySurveyAppService.RemoveLineAsync(surveyId, lineId, cancellationToken);

    /// <summary>Total fine exposure computed from the answered lines.</summary>
    [HttpGet("surveys/{surveyId:int}/total")]
    [ProducesResponseType<PenaltySurveyTotalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PenaltySurveyTotalDto> CalculateSurveyTotalAsync(
        int surveyId,
        CancellationToken cancellationToken)
        => penaltySurveyAppService.CalculateTotalAsync(surveyId, cancellationToken);
}
