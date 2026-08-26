using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Reports.Dtos;
using Ensa.Application.Contracts.Reports.Dtos.Navigations;

namespace Ensa.Application.Contracts.Reports;

/// <summary>
/// Periodic activity reports produced for a workplace: header plus typed data rows.
/// <para>
/// <b>This service generates nothing.</b> It reads and persists report records. Computing what
/// belongs in a report — counting visits, trainings, unexamined equipment and so on — is the job
/// of a reporting engine that is out of scope here; this service is the storage and retrieval
/// side of that engine, and the rows it accepts are whatever the engine (or a user) supplies.
/// </para>
/// </summary>
public interface IActivityReportAppService : IApplicationService
{
    Task<ActivityReportDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The report with its workplace and its data rows.</summary>
    Task<ActivityReportNavigationDto> GetWithNavigationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ActivityReportListDto>> GetListAsync(
        GetActivityReportListInput input,
        CancellationToken cancellationToken = default);

    Task<ActivityReportDto> CreateAsync(
        CreateActivityReportDto input,
        CancellationToken cancellationToken = default);

    Task<ActivityReportDto> UpdateAsync(
        int id,
        UpdateActivityReportDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the report together with its data rows.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ------------------------------------------------------------------ Lines

    Task<ListResultDto<ActivityReportLineDto>> GetLinesAsync(
        int reportId,
        CancellationToken cancellationToken = default);

    Task<ActivityReportLineDto> AddLineAsync(
        int reportId,
        CreateActivityReportLineDto input,
        CancellationToken cancellationToken = default);

    Task<ActivityReportLineDto> UpdateLineAsync(
        int reportId,
        int lineId,
        UpdateActivityReportLineDto input,
        CancellationToken cancellationToken = default);

    Task RemoveLineAsync(int reportId, int lineId, CancellationToken cancellationToken = default);
}
