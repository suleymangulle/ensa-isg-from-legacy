using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Reports.Dtos;
using Ensa.Application.Contracts.Reports.Dtos.Navigations;

namespace Ensa.Application.Contracts.Reports;

/// <summary>
/// Year-end review reports: an annual assessment written for a workplace, whose work items form
/// a tree (a top-level activity with its sub-activities beneath it).
/// <para>
/// <b>This service generates nothing.</b> It reads and persists report records; producing the
/// content of an annual assessment is out of scope here.
/// </para>
/// </summary>
public interface IYearEndReviewReportAppService : IApplicationService
{
    Task<YearEndReviewReportDto> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The report with its workplace and the complete work item tree. The repository builds the
    /// tree in a fixed number of queries, so depth costs nothing extra.
    /// </summary>
    Task<YearEndReviewReportNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<YearEndReviewReportListDto>> GetListAsync(
        GetYearEndReviewReportListInput input,
        CancellationToken cancellationToken = default);

    /// <summary>The most recent report of a workplace, or <c>null</c> when it has none.</summary>
    Task<YearEndReviewReportDto?> GetCurrentAsync(int companyId, CancellationToken cancellationToken = default);

    Task<YearEndReviewReportDto> CreateAsync(
        CreateYearEndReviewReportDto input,
        CancellationToken cancellationToken = default);

    Task<YearEndReviewReportDto> UpdateAsync(
        int id,
        UpdateYearEndReviewReportDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the report together with every work item in its tree.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // -------------------------------------------------------------- Work items

    /// <summary>Every work item of the report, flat, in parent-then-order sequence.</summary>
    Task<ListResultDto<YearEndReviewLineDto>> GetLinesAsync(
        int reportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a work item. A supplied parent must belong to the same report, otherwise the tree
    /// would span two reports and the navigation view could never be built consistently.
    /// </summary>
    Task<YearEndReviewLineDto> AddLineAsync(
        int reportId,
        CreateYearEndReviewLineDto input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a work item. Re-parenting is validated against self-reference and against cycles,
    /// since a cycle would make the tree walk non-terminating.
    /// </summary>
    Task<YearEndReviewLineDto> UpdateLineAsync(
        int reportId,
        int lineId,
        UpdateYearEndReviewLineDto input,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a work item together with its whole subtree.</summary>
    Task RemoveLineAsync(int reportId, int lineId, CancellationToken cancellationToken = default);
}
