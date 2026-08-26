using Ensa.Domain.Companies;
using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Reports;
using Ensa.Application.Contracts.Reports.Dtos;
using Ensa.Application.Contracts.Reports.Dtos.Navigations;
using Ensa.Domain.Reports;
using Ensa.Domain.Reports.Navigations;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Reports;

/// <summary>
/// Year-end review report application service.
/// <para>
/// <b>This service generates nothing.</b> It reads and persists report records; producing the
/// content of an annual assessment is out of scope here.
/// </para>
/// <para>
/// Work items form a tree through <c>ParentLineId</c>. The read path does not walk that tree
/// itself: <c>IYearEndReviewReportRepository.GetWithNavigationAsync</c> already assembles it in a
/// fixed number of queries regardless of depth, so this service only projects what it returns.
/// The write path is where the tree invariants are enforced — a parent must live in the same
/// report, and re-parenting must not create a cycle, or the tree walk would never terminate.
/// </para>
/// </summary>
public class YearEndReviewReportAppService(
    IServiceProvider serviceProvider,
    IYearEndReviewReportRepository yearEndReviewReportRepository,
    IRepository<YearEndReviewLine> yearEndReviewLineRepository,
    IReadOnlyRepository<Company> companyRepository)
    : EnsaAppService(serviceProvider), IYearEndReviewReportAppService
{
    /// <inheritdoc />
    public async Task<YearEndReviewReportDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Default);

        var report = await yearEndReviewReportRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(YearEndReviewReport), id);

        return ObjectMapper.Map<YearEndReviewReport, YearEndReviewReportDto>(report);
    }

    /// <inheritdoc />
    public async Task<YearEndReviewReportNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Default);

        var navigation = await yearEndReviewReportRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(YearEndReviewReport), id);

        return new YearEndReviewReportNavigationDto
        {
            YearEndReviewReport = ObjectMapper
                .Map<YearEndReviewReport, YearEndReviewReportDto>(navigation.YearEndReviewReport),
            Company = navigation.Company is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Company.Id,
                    DisplayName = navigation.Company.CompanyName,
                    Code = navigation.Company.SsiNumber,
                    IsActive = navigation.Company.IsActive
                },
            Activities = MapTree(navigation.Activities)
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<YearEndReviewReportListDto>> GetListAsync(
        GetYearEndReviewReportListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Report.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "ReportDate DESC");

        var total = await yearEndReviewReportRepository.GetCountAsync(predicate, cancellationToken);

        var records = await yearEndReviewReportRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper
            .Map<List<YearEndReviewReport>, List<YearEndReviewReportListDto>>(records);

        if (items.Count > 0)
        {
            // One batched query for the whole page, not one per row.
            var companyIds = records.Select(r => r.CompanyId).Distinct().ToList();

            var companyNames = (await companyRepository
                    .GetListAsync(c => companyIds.Contains(c.Id), cancellationToken))
                .ToDictionary(c => c.Id, c => c.CompanyName);

            foreach (var item in items)
            {
                if (companyNames.TryGetValue(item.CompanyId, out var companyName))
                {
                    item.CompanyName = companyName;
                }
            }
        }
        return new PagedResultDto<YearEndReviewReportListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<YearEndReviewReportDto?> GetCurrentAsync(
        int companyId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Default);

        var report = await yearEndReviewReportRepository.GetCurrentReportAsync(companyId, cancellationToken);

        return report is null
            ? null
            : ObjectMapper.Map<YearEndReviewReport, YearEndReviewReportDto>(report);
    }

    /// <inheritdoc />
    public async Task<YearEndReviewReportDto> CreateAsync(
        CreateYearEndReviewReportDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Report.Create);

        var report = ObjectMapper.Map<CreateYearEndReviewReportDto, YearEndReviewReport>(input);
        report.IsActive = true;

        report = await yearEndReviewReportRepository.InsertAsync(report, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Year-end review report created: {ReportId} — {ReportTitle}",
            report.Id,
            report.ReportTitle);

        return ObjectMapper.Map<YearEndReviewReport, YearEndReviewReportDto>(report);
    }

    /// <inheritdoc />
    public async Task<YearEndReviewReportDto> UpdateAsync(
        int id,
        UpdateYearEndReviewReportDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Report.Update);

        var report = await yearEndReviewReportRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(YearEndReviewReport), id);

        ObjectMapper.Map(input, report);

        report = await yearEndReviewReportRepository.UpdateAsync(report, autoSave: true, cancellationToken);

        return ObjectMapper.Map<YearEndReviewReport, YearEndReviewReportDto>(report);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Delete);

        var report = await yearEndReviewReportRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(YearEndReviewReport), id);

        var lines = await yearEndReviewLineRepository.GetListAsync(
            s => s.YearEndReviewReportId == id,
            cancellationToken);

        if (lines.Count > 0)
        {
            await yearEndReviewLineRepository.DeleteManyAsync(lines, autoSave: false, cancellationToken);
        }

        await yearEndReviewReportRepository.DeleteAsync(report, autoSave: true, cancellationToken);

        Logger.LogInformation("Year-end review report deleted: {ReportId}", id);
    }

    // ------------------------------------------------------------ Work items

    /// <inheritdoc />
    public async Task<ListResultDto<YearEndReviewLineDto>> GetLinesAsync(
        int reportId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Default);

        _ = await yearEndReviewReportRepository.FindAsync(reportId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(YearEndReviewReport), reportId);

        var lines = await GetReportLinesAsync(reportId, cancellationToken);

        var items = ObjectMapper
            .Map<List<YearEndReviewLine>, List<YearEndReviewLineDto>>(lines)
            .OrderBy(s => s.ParentLineId ?? 0)
            .ThenBy(s => s.OrderNo)
            .ThenBy(s => s.Id)
            .ToList();

        return new ListResultDto<YearEndReviewLineDto>(items);
    }

    /// <inheritdoc />
    public async Task<YearEndReviewLineDto> AddLineAsync(
        int reportId,
        CreateYearEndReviewLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Report.Create);

        _ = await yearEndReviewReportRepository.FindAsync(reportId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(YearEndReviewReport), reportId);

        var siblings = await GetReportLinesAsync(reportId, cancellationToken);

        if (input.ParentLineId is { } parentId)
        {
            EnsureParentInSameReport(siblings, parentId, reportId);
        }

        var line = ObjectMapper.Map<CreateYearEndReviewLineDto, YearEndReviewLine>(input);
        line.YearEndReviewReportId = reportId;
        line.ParentLineId = input.ParentLineId;
        line.IsActive = true;

        if (line.OrderNo <= 0)
        {
            var sameLevel = siblings.Where(s => s.ParentLineId == input.ParentLineId).ToList();
            line.OrderNo = sameLevel.Count == 0 ? 1 : sameLevel.Max(s => s.OrderNo) + 1;
        }

        line = await yearEndReviewLineRepository.InsertAsync(line, autoSave: true, cancellationToken);

        return ObjectMapper.Map<YearEndReviewLine, YearEndReviewLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<YearEndReviewLineDto> UpdateLineAsync(
        int reportId,
        int lineId,
        UpdateYearEndReviewLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Report.Update);

        _ = await yearEndReviewReportRepository.FindAsync(reportId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(YearEndReviewReport), reportId);

        var lines = await GetReportLinesAsync(reportId, cancellationToken);

        var line = lines.Find(s => s.Id == lineId)
                   ?? throw new EntityNotFoundException(typeof(YearEndReviewLine), lineId);

        if (input.ParentLineId is { } parentId)
        {
            if (parentId == lineId)
            {
                throw new BusinessException(
                        "A work item cannot be its own parent.",
                        "Ensa:Report:CircularLineHierarchy")
                    .WithData("LineId", lineId);
            }

            EnsureParentInSameReport(lines, parentId, reportId);
            EnsureNoCycle(lines, lineId, parentId);
        }

        var orderNo = line.OrderNo;

        ObjectMapper.Map(input, line);
        line.YearEndReviewReportId = reportId;
        line.ParentLineId = input.ParentLineId;
        line.OrderNo = input.OrderNo > 0 ? input.OrderNo : orderNo;

        line = await yearEndReviewLineRepository.UpdateAsync(line, autoSave: true, cancellationToken);

        return ObjectMapper.Map<YearEndReviewLine, YearEndReviewLineDto>(line);
    }

    /// <inheritdoc />
    public async Task RemoveLineAsync(
        int reportId,
        int lineId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Delete);

        _ = await yearEndReviewReportRepository.FindAsync(reportId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(YearEndReviewReport), reportId);

        var lines = await GetReportLinesAsync(reportId, cancellationToken);

        var line = lines.Find(s => s.Id == lineId)
                   ?? throw new EntityNotFoundException(typeof(YearEndReviewLine), lineId);

        // Children are removed with the parent. Leaving them behind would strand them under a
        // parent id that no longer resolves, and they would silently vanish from the tree view
        // while still occupying rows.
        var subtree = CollectSubtree(lines, line);

        await yearEndReviewLineRepository.DeleteManyAsync(subtree, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Year-end review work item removed: {LineId} ({SubtreeCount} rows)",
            lineId,
            subtree.Count);
    }

    // -----------------------------------------------------------------

    private Task<List<YearEndReviewLine>> GetReportLinesAsync(
        int reportId,
        CancellationToken cancellationToken)
        => yearEndReviewLineRepository.GetListAsync(
            s => s.YearEndReviewReportId == reportId,
            cancellationToken);

    private List<YearEndReviewLineNavigationDto> MapTree(List<YearEndReviewLineNavigation> nodes)
        =>
        [
            .. nodes
                .OrderBy(n => n.Line.OrderNo)
                .ThenBy(n => n.Line.Id)
                .Select(n => new YearEndReviewLineNavigationDto
                {
                    Line = ObjectMapper.Map<YearEndReviewLine, YearEndReviewLineDto>(n.Line),
                    ChildActivities = MapTree(n.ChildActivities)
                })
        ];

    private static void EnsureParentInSameReport(
        List<YearEndReviewLine> reportLines,
        int parentId,
        int reportId)
    {
        if (!reportLines.Exists(s => s.Id == parentId))
        {
            throw new BusinessException(
                    "The parent work item does not belong to this report.",
                    "Ensa:Report:ParentLineMismatch")
                .WithData("ReportId", reportId)
                .WithData("ParentLineId", parentId);
        }
    }

    /// <summary>
    /// Refuses a re-parent that would put a work item underneath one of its own descendants.
    /// Walking up from the proposed parent must reach the root without meeting the item itself.
    /// </summary>
    private static void EnsureNoCycle(List<YearEndReviewLine> reportLines, int lineId, int parentId)
    {
        var cursor = reportLines.Find(s => s.Id == parentId);

        // The report's own row count bounds the walk, so a pre-existing cycle in stored data
        // cannot turn this guard into an infinite loop.
        for (var step = 0; cursor is not null && step <= reportLines.Count; step++)
        {
            if (cursor.Id == lineId)
            {
                throw new BusinessException(
                        "The selected parent is a descendant of this work item, which would create a cycle.",
                        "Ensa:Report:CircularLineHierarchy")
                    .WithData("LineId", lineId)
                    .WithData("ParentLineId", parentId);
            }

            cursor = cursor.ParentLineId is { } nextId
                ? reportLines.Find(s => s.Id == nextId)
                : null;
        }
    }

    /// <summary>Returns the item together with every descendant beneath it.</summary>
    private static List<YearEndReviewLine> CollectSubtree(
        List<YearEndReviewLine> reportLines,
        YearEndReviewLine root)
    {
        var collected = new List<YearEndReviewLine> { root };
        var pending = new Queue<int>();
        pending.Enqueue(root.Id);

        while (pending.Count > 0)
        {
            var currentId = pending.Dequeue();

            foreach (var child in reportLines.Where(s => s.ParentLineId == currentId))
            {
                if (collected.Exists(s => s.Id == child.Id))
                {
                    continue;
                }

                collected.Add(child);
                pending.Enqueue(child.Id);
            }
        }

        return collected;
    }

    private static Expression<Func<YearEndReviewReport, bool>>? BuildFilter(
        GetYearEndReviewReportListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var companyId = input.CompanyId;
        var specialistId = input.SpecialistUserId;
        var physicianId = input.PhysicianUserId;
        var isActive = input.IsActive;
        var startDate = input.StartDate;
        var endDate = input.EndDate;

        if (search is null
            && companyId is null
            && specialistId is null
            && physicianId is null
            && isActive is null
            && startDate is null
            && endDate is null)
        {
            return null;
        }

        return r =>
            (search == null || r.ReportTitle.Contains(search))
            && (companyId == null || r.CompanyId == companyId)
            && (specialistId == null || r.SpecialistUserId == specialistId)
            && (physicianId == null || r.PhysicianUserId == physicianId)
            && (isActive == null || r.IsActive == isActive)
            && (startDate == null || r.ReportDate >= startDate)
            && (endDate == null || r.ReportDate <= endDate);
    }
}
