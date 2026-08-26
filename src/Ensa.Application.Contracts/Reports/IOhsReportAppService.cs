using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Reports.Dtos;

namespace Ensa.Application.Contracts.Reports;

/// <summary>
/// OHS service-time reports: how much assigned time each specialist or physician used in a
/// period, and across how many workplaces of each hazard class.
/// <para>
/// <b>Read-only, and this service generates nothing.</b> These records are produced by the
/// reporting engine from assignment and work-plan data; computing them is out of scope here, so
/// there is deliberately no create, update or delete on this contract.
/// </para>
/// </summary>
public interface IOhsReportAppService : IApplicationService
{
    Task<OhsReportDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<OhsReportListDto>> GetListAsync(
        GetOhsReportListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Reports of one office in a period, from the repository's own query.</summary>
    Task<ListResultDto<OhsReportDto>> GetOfficeReportsAsync(
        int officeId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hazard-class distribution of the workplaces covered by a report, read from the normalized
    /// child table. Every hazard class is returned, with zero where the report has no row, so a
    /// chart does not have to guess which buckets are missing.
    /// </summary>
    Task<ListResultDto<OhsReportHazardClassBreakdownDto>> GetHazardClassBreakdownAsync(
        int reportId,
        CancellationToken cancellationToken = default);
}
