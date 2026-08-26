using Ensa.Domain.Companies;
using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Reports;
using Ensa.Application.Contracts.Reports.Dtos;
using Ensa.Application.Contracts.Reports.Dtos.Navigations;
using Ensa.Domain.Reports;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Reports;

/// <summary>
/// Activity report application service.
/// <para>
/// <b>This service generates nothing.</b> It reads and persists report records. Working out what
/// belongs in a report — counting visits, trainings, unexamined equipment, incidents and so on —
/// is the job of a reporting engine that is out of scope here. This service is the storage side
/// of that engine: the rows it accepts are whatever the engine, or a user, supplies.
/// </para>
/// </summary>
public class ActivityReportAppService(
    IServiceProvider serviceProvider,
    IActivityReportRepository activityReportRepository,
    IRepository<ActivityReportLine> activityReportLineRepository,
    IReadOnlyRepository<Company> companyRepository)
    : EnsaAppService(serviceProvider), IActivityReportAppService
{
    /// <inheritdoc />
    public async Task<ActivityReportDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Default);

        var report = await activityReportRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(ActivityReport), id);

        return ObjectMapper.Map<ActivityReport, ActivityReportDto>(report);
    }

    /// <inheritdoc />
    public async Task<ActivityReportNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Default);

        var navigation = await activityReportRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(ActivityReport), id);

        return new ActivityReportNavigationDto
        {
            ActivityReport = ObjectMapper.Map<ActivityReport, ActivityReportDto>(navigation.ActivityReport),
            Company = navigation.Company is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Company.Id,
                    DisplayName = navigation.Company.CompanyName,
                    Code = navigation.Company.SsiNumber,
                    IsActive = navigation.Company.IsActive
                },
            Lines =
            [
                .. ObjectMapper
                    .Map<List<ActivityReportLine>, List<ActivityReportLineDto>>(navigation.Lines)
                    .OrderBy(s => s.OrderNo)
                    .ThenBy(s => s.Id)
            ]
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<ActivityReportListDto>> GetListAsync(
        GetActivityReportListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Report.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "ReportStart DESC");

        var total = await activityReportRepository.GetCountAsync(predicate, cancellationToken);

        var records = await activityReportRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<ActivityReport>, List<ActivityReportListDto>>(records);

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
        return new PagedResultDto<ActivityReportListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ActivityReportDto> CreateAsync(
        CreateActivityReportDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Report.Create);

        EnsurePeriodValid(input.ReportStart, input.ReportEnd);

        var report = ObjectMapper.Map<CreateActivityReportDto, ActivityReport>(input);

        report = await activityReportRepository.InsertAsync(report, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Activity report created: {ReportId} — {ReportName}",
            report.Id,
            report.ReportName);

        return ObjectMapper.Map<ActivityReport, ActivityReportDto>(report);
    }

    /// <inheritdoc />
    public async Task<ActivityReportDto> UpdateAsync(
        int id,
        UpdateActivityReportDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Report.Update);

        EnsurePeriodValid(input.ReportStart, input.ReportEnd);

        var report = await activityReportRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(ActivityReport), id);

        ObjectMapper.Map(input, report);

        report = await activityReportRepository.UpdateAsync(report, autoSave: true, cancellationToken);

        return ObjectMapper.Map<ActivityReport, ActivityReportDto>(report);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Delete);

        var report = await activityReportRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(ActivityReport), id);

        var lines = await activityReportLineRepository.GetListAsync(
            s => s.ActivityReportId == id,
            cancellationToken);

        if (lines.Count > 0)
        {
            await activityReportLineRepository.DeleteManyAsync(lines, autoSave: false, cancellationToken);
        }

        await activityReportRepository.DeleteAsync(report, autoSave: true, cancellationToken);

        Logger.LogInformation("Activity report deleted: {ReportId}", id);
    }

    // ---------------------------------------------------------------- Lines

    /// <inheritdoc />
    public async Task<ListResultDto<ActivityReportLineDto>> GetLinesAsync(
        int reportId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Default);

        _ = await activityReportRepository.FindAsync(reportId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(ActivityReport), reportId);

        var lines = await activityReportLineRepository.GetListAsync(
            s => s.ActivityReportId == reportId,
            cancellationToken);

        var items = ObjectMapper
            .Map<List<ActivityReportLine>, List<ActivityReportLineDto>>(lines)
            .OrderBy(s => s.OrderNo)
            .ThenBy(s => s.Id)
            .ToList();

        return new ListResultDto<ActivityReportLineDto>(items);
    }

    /// <inheritdoc />
    public async Task<ActivityReportLineDto> AddLineAsync(
        int reportId,
        CreateActivityReportLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Report.Create);

        _ = await activityReportRepository.FindAsync(reportId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(ActivityReport), reportId);

        var line = ObjectMapper.Map<CreateActivityReportLineDto, ActivityReportLine>(input);
        line.ActivityReportId = reportId;

        if (line.OrderNo <= 0)
        {
            var existing = await activityReportLineRepository.GetListAsync(
                s => s.ActivityReportId == reportId,
                cancellationToken);

            line.OrderNo = existing.Count == 0 ? 1 : existing.Max(s => s.OrderNo) + 1;
        }

        line = await activityReportLineRepository.InsertAsync(line, autoSave: true, cancellationToken);

        return ObjectMapper.Map<ActivityReportLine, ActivityReportLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<ActivityReportLineDto> UpdateLineAsync(
        int reportId,
        int lineId,
        UpdateActivityReportLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Report.Update);

        var line = await GetLineOfReportAsync(reportId, lineId, cancellationToken);

        var orderNo = line.OrderNo;

        ObjectMapper.Map(input, line);
        line.ActivityReportId = reportId;
        line.OrderNo = input.OrderNo > 0 ? input.OrderNo : orderNo;

        line = await activityReportLineRepository.UpdateAsync(line, autoSave: true, cancellationToken);

        return ObjectMapper.Map<ActivityReportLine, ActivityReportLineDto>(line);
    }

    /// <inheritdoc />
    public async Task RemoveLineAsync(
        int reportId,
        int lineId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Delete);

        var line = await GetLineOfReportAsync(reportId, lineId, cancellationToken);

        await activityReportLineRepository.DeleteAsync(line, autoSave: true, cancellationToken);
    }

    // -----------------------------------------------------------------

    private async Task<ActivityReportLine> GetLineOfReportAsync(
        int reportId,
        int lineId,
        CancellationToken cancellationToken)
    {
        _ = await activityReportRepository.FindAsync(reportId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(ActivityReport), reportId);

        var line = await activityReportLineRepository.FindAsync(lineId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(ActivityReportLine), lineId);

        if (line.ActivityReportId != reportId)
        {
            throw new BusinessException(
                    "The line does not belong to this report.",
                    "Ensa:Report:LineNotInReport")
                .WithData("ReportId", reportId);
        }

        return line;
    }

    private static void EnsurePeriodValid(DateTime start, DateTime end)
    {
        if (start > end)
        {
            throw new BusinessException(
                "The start of the reporting period must not be later than its end.",
                "Ensa:Report:InvalidPeriod");
        }
    }

    private static Expression<Func<ActivityReport, bool>>? BuildFilter(GetActivityReportListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var companyId = input.CompanyId;
        var reportType = input.ReportType;
        var startDate = input.StartDate;
        var endDate = input.EndDate;

        if (search is null
            && companyId is null
            && reportType is null
            && startDate is null
            && endDate is null)
        {
            return null;
        }

        return r =>
            (search == null || r.ReportName.Contains(search))
            && (companyId == null || r.CompanyId == companyId)
            && (reportType == null || r.ReportType == reportType)
            && (startDate == null || r.ReportEnd >= startDate)
            && (endDate == null || r.ReportStart <= endDate);
    }
}
