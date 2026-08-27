using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Risks;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Risk assessment report endpoints — <c>api/risk-assessment-report</c>.
/// <para>Authorization is enforced by policy; errors are shaped by <c>EnsaExceptionFilter</c>.</para>
/// </summary>
public class RiskAssessmentReportController(IRiskAssessmentReportAppService appService) : EnsaController
{
    /// <summary>Returns a single risk assessment report header.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<RiskAssessmentReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<RiskAssessmentReportDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>Combined detail view: report, company, signatories and every child collection.</summary>
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType<RiskAssessmentReportNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<RiskAssessmentReportNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken)
        => appService.GetWithNavigationAsync(id, cancellationToken);

    /// <summary>Paged, filterable report list.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<RiskAssessmentReportListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<RiskAssessmentReportListDto>> GetListAsync(
        [FromQuery] GetRiskAssessmentReportListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);

    /// <summary>Reports already expired at the given date or expiring within the window.</summary>
    [HttpGet("expiring")]
    [ProducesResponseType<ListResultDto<RiskAssessmentReportListDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<RiskAssessmentReportListDto>> GetExpiringAsync(
        [FromQuery] DateTime asOf,
        [FromQuery] int withinDays,
        [FromQuery] int? companyId,
        CancellationToken cancellationToken)
        => appService.GetExpiringAsync(asOf, withinDays, companyId, cancellationToken);

    /// <summary>The report currently in force for a company.</summary>
    [HttpGet("active/{companyId:int}")]
    [ProducesResponseType<RiskAssessmentReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<RiskAssessmentReportDto?> GetActiveForCompanyAsync(
        int companyId,
        CancellationToken cancellationToken)
        => appService.GetActiveForCompanyAsync(companyId, cancellationToken);

    /// <summary>Creates a new risk assessment report.</summary>
    [HttpPost]
    [ProducesResponseType<RiskAssessmentReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<RiskAssessmentReportDto> CreateAsync(
        [FromBody] CreateRiskAssessmentReportDto input,
        CancellationToken cancellationToken)
        => appService.CreateAsync(input, cancellationToken);

    /// <summary>Updates an existing risk assessment report.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<RiskAssessmentReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<RiskAssessmentReportDto> UpdateAsync(
        int id,
        [FromBody] UpdateRiskAssessmentReportDto input,
        CancellationToken cancellationToken)
        => appService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Deletes the report together with its child records (soft delete).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
        => appService.DeleteAsync(id, cancellationToken);

    // ------------------------------------------------------- Identified hazards

    /// <summary>Adds a hazard line; the risk score is computed by the domain manager.</summary>
    [HttpPost("{id:int}/hazards")]
    [ProducesResponseType<IdentifiedHazardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IdentifiedHazardDto> AddIdentifiedHazardAsync(
        int id,
        [FromBody] CreateIdentifiedHazardDto input,
        CancellationToken cancellationToken)
        => appService.AddIdentifiedHazardAsync(id, input, cancellationToken);

    /// <summary>Updates a hazard line and re-scores it.</summary>
    [HttpPut("{id:int}/hazards/{hazardId:int}")]
    [ProducesResponseType<IdentifiedHazardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IdentifiedHazardDto> UpdateIdentifiedHazardAsync(
        int id,
        int hazardId,
        [FromBody] UpdateIdentifiedHazardDto input,
        CancellationToken cancellationToken)
        => appService.UpdateIdentifiedHazardAsync(id, hazardId, input, cancellationToken);

    /// <summary>Removes a hazard line and its control measures.</summary>
    [HttpDelete("{id:int}/hazards/{hazardId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RemoveIdentifiedHazardAsync(int id, int hazardId, CancellationToken cancellationToken)
        => appService.RemoveIdentifiedHazardAsync(id, hazardId, cancellationToken);

    // -------------------------------------------------- Hazard control measures

    /// <summary>Adds a control measure to a hazard line.</summary>
    [HttpPost("hazards/{hazardId:int}/control-measures")]
    [ProducesResponseType<ControlMeasureDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ControlMeasureDto> AddControlMeasureAsync(
        int hazardId,
        [FromBody] CreateControlMeasureDto input,
        CancellationToken cancellationToken)
        => appService.AddControlMeasureAsync(hazardId, input, cancellationToken);

    /// <summary>Marks a control measure as completed.</summary>
    [HttpPost("control-measures/{controlMeasureId:int}/complete")]
    [ProducesResponseType<ControlMeasureDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ControlMeasureDto> CompleteControlMeasureAsync(
        int controlMeasureId,
        [FromQuery] DateTime completionDate,
        CancellationToken cancellationToken)
        => appService.CompleteControlMeasureAsync(controlMeasureId, completionDate, cancellationToken);

    // -------------------------------------------------- Header checkbox groups

    /// <summary>Replaces the exposed person groups flagged on the header.</summary>
    [HttpPut("{id:int}/exposed-groups")]
    [ProducesResponseType<ListResultDto<RiskAssessmentExposedGroupDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<RiskAssessmentExposedGroupDto>> SetExposedGroupsAsync(
        int id,
        [FromBody] List<ExposedPersonGroup> groups,
        CancellationToken cancellationToken)
        => appService.SetExposedGroupsAsync(id, groups, cancellationToken);

    /// <summary>Replaces the existing control measures flagged on the header.</summary>
    [HttpPut("{id:int}/existing-control-measures")]
    [ProducesResponseType<ListResultDto<RiskAssessmentControlMeasureDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<RiskAssessmentControlMeasureDto>> SetExistingControlMeasuresAsync(
        int id,
        [FromBody] List<ExistingControlMeasure> measures,
        CancellationToken cancellationToken)
        => appService.SetExistingControlMeasuresAsync(id, measures, cancellationToken);

    /// <summary>Replaces the improvement recommendations flagged on the header.</summary>
    [HttpPut("{id:int}/improvement-actions")]
    [ProducesResponseType<ListResultDto<RiskAssessmentImprovementActionDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<RiskAssessmentImprovementActionDto>> SetImprovementActionsAsync(
        int id,
        [FromBody] List<ImprovementAction> recommendations,
        CancellationToken cancellationToken)
        => appService.SetImprovementActionsAsync(id, recommendations, cancellationToken);
}
