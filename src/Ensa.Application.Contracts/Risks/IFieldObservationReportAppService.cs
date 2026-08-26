using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;

namespace Ensa.Application.Contracts.Risks;

/// <summary>Field observation (workplace inspection tour) report application service.</summary>
public interface IFieldObservationReportAppService : IApplicationService
{
    Task<FieldObservationReportDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detail projection: report, company, department and every line with its document,
    /// responsible employee and derived corrective actions.
    /// </summary>
    Task<FieldObservationReportNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<FieldObservationReportListDto>> GetListAsync(
        GetFieldObservationReportListInput input,
        CancellationToken cancellationToken = default);

    Task<FieldObservationReportDto> CreateAsync(
        CreateFieldObservationReportDto input,
        CancellationToken cancellationToken = default);

    Task<FieldObservationReportDto> UpdateAsync(
        int id,
        UpdateFieldObservationReportDto input,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ------------------------------------------------------------------- Lines

    Task<ListResultDto<FieldObservationLineDto>> GetLinesAsync(
        int reportId,
        CancellationToken cancellationToken = default);

    Task<FieldObservationLineDto> AddLineAsync(
        int reportId,
        CreateFieldObservationLineDto input,
        CancellationToken cancellationToken = default);

    Task<FieldObservationLineDto> UpdateLineAsync(
        int reportId,
        int lineId,
        UpdateFieldObservationLineDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Refuses to remove a line that still has corrective actions derived from it.</summary>
    Task RemoveLineAsync(
        int reportId,
        int lineId,
        CancellationToken cancellationToken = default);
}
