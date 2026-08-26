using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Risks;

/// <summary>
/// Risk assessment report application service.
/// <para>
/// Every risk score, risk level and validity date is produced by
/// <c>IRiskAssessmentManager</c>; this service never recomputes them.
/// </para>
/// </summary>
public interface IRiskAssessmentReportAppService : IApplicationService
{
    Task<RiskAssessmentReportDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Detail screen projection: report, company, signatories and every child collection.</summary>
    Task<RiskAssessmentReportNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<RiskAssessmentReportListDto>> GetListAsync(
        GetRiskAssessmentReportListInput input,
        CancellationToken cancellationToken = default);

    Task<RiskAssessmentReportDto> CreateAsync(
        CreateRiskAssessmentReportDto input,
        CancellationToken cancellationToken = default);

    Task<RiskAssessmentReportDto> UpdateAsync(
        int id,
        UpdateRiskAssessmentReportDto input,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports whose validity has already lapsed at <paramref name="asOf"/> or lapses within
    /// <paramref name="withinDays"/> days of it.
    /// </summary>
    Task<ListResultDto<RiskAssessmentReportListDto>> GetExpiringAsync(
        DateTime asOf,
        int withinDays = 30,
        int? companyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>The report currently in force for a company; <c>null</c> when none is valid.</summary>
    Task<RiskAssessmentReportDto?> GetActiveForCompanyAsync(
        int companyId,
        CancellationToken cancellationToken = default);

    // ------------------------------------------------------- Identified hazards

    /// <summary>Adds a hazard line and lets <c>IRiskAssessmentManager</c> score it.</summary>
    Task<IdentifiedHazardDto> AddIdentifiedHazardAsync(
        int reportId,
        CreateIdentifiedHazardDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Updates a hazard line and re-scores it through <c>IRiskAssessmentManager</c>.</summary>
    Task<IdentifiedHazardDto> UpdateIdentifiedHazardAsync(
        int reportId,
        int hazardId,
        UpdateIdentifiedHazardDto input,
        CancellationToken cancellationToken = default);

    Task RemoveIdentifiedHazardAsync(
        int reportId,
        int hazardId,
        CancellationToken cancellationToken = default);

    // -------------------------------------------------- Hazard control measures

    /// <summary>Adds a control measure to an identified hazard line.</summary>
    Task<ControlMeasureDto> AddControlMeasureAsync(
        int hazardId,
        CreateControlMeasureDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a control measure as completed on the given date.</summary>
    Task<ControlMeasureDto> CompleteControlMeasureAsync(
        int controlMeasureId,
        DateTime completionDate,
        CancellationToken cancellationToken = default);

    // ------------------------------------------------- Header checkbox groups

    /// <summary>Replaces the exposed person groups flagged on the report header.</summary>
    Task<ListResultDto<RiskAssessmentExposedGroupDto>> SetExposedGroupsAsync(
        int reportId,
        List<ExposedPersonGroup> groups,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces the existing control measures flagged on the report header.</summary>
    Task<ListResultDto<RiskAssessmentControlMeasureDto>> SetExistingControlMeasuresAsync(
        int reportId,
        List<ExistingControlMeasure> measures,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces the improvement recommendations flagged on the report header.</summary>
    Task<ListResultDto<RiskAssessmentImprovementActionDto>> SetImprovementActionsAsync(
        int reportId,
        List<ImprovementAction> recommendations,
        CancellationToken cancellationToken = default);
}
