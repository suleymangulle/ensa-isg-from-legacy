using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Risks;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Ensa.Domain.Companies;
using Ensa.Domain.Repositories;
using Ensa.Domain.Risks;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Risks;

/// <summary>
/// Field observation (workplace inspection tour) report application service.
/// <para>
/// The report is a header with non-conformity lines. Lines are managed through dedicated
/// endpoints rather than a nested collection, because input DTOs may not carry class-typed
/// properties (see docs/ARCHITECTURE.md).
/// </para>
/// </summary>
public class FieldObservationReportAppService(
    IServiceProvider serviceProvider,
    IFieldObservationReportRepository reportRepository,
    IRepository<FieldObservationLine> lineRepository,
    ICorrectiveActionRepository correctiveActionRepository,
    IReadOnlyRepository<Company> companyRepository,
    IReadOnlyRepository<WorkplaceDepartment> departmentRepository)
    : EnsaAppService(serviceProvider), IFieldObservationReportAppService
{
    /// <inheritdoc />
    public async Task<FieldObservationReportDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.FieldObservation.Default);

        var report = await reportRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(FieldObservationReport), id);

        return ObjectMapper.Map<FieldObservationReport, FieldObservationReportDto>(report);
    }

    /// <inheritdoc />
    public async Task<FieldObservationReportNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.FieldObservation.Default);

        // One repository call already joins department, lines, line documents, owners and the
        // corrective actions derived from each line — the projection issues no further queries.
        var navigation = await reportRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(FieldObservationReport), id);

        var reference = Clock.Now.Date;

        return new FieldObservationReportNavigationDto
        {
            Report = ObjectMapper.Map<FieldObservationReport, FieldObservationReportDto>(navigation.Report),
            Company = RiskLookupHelper.Lookup(navigation.Company?.Id, navigation.Company?.CompanyName),
            Department = RiskLookupHelper.Lookup(
                navigation.Department?.Id, navigation.Department?.DepartmentName),
            Lines =
            [
                .. navigation.Lines.Select(l => new FieldObservationLineNavigationDto
                {
                    Line = MapLine(l.Line, reference),
                    Document = RiskLookupHelper.Lookup(l.Document?.Id, l.Document?.DocumentName),
                    OwnerEmployee = RiskLookupHelper.Lookup(
                        l.OwnerEmployee?.Id,
                        l.OwnerEmployee is { } employee ? $"{employee.Name} {employee.LastName}".Trim() : null),
                    CorrectiveActions = ObjectMapper
                        .Map<List<CorrectiveAction>, List<CorrectiveActionDto>>(l.CorrectiveActions)
                })
            ]
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<FieldObservationReportListDto>> GetListAsync(
        GetFieldObservationReportListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.FieldObservation.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "Date DESC");

        var total = await reportRepository.GetCountAsync(predicate, cancellationToken);

        var records = await reportRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<FieldObservationReport>, List<FieldObservationReportListDto>>(records);

        // Three batched queries for the whole page — company names, department names and the
        // line rows counted in memory. Never one query per row.
        var companyNames = await RiskLookupHelper.LoadCompanyNamesAsync(
            companyRepository,
            RiskLookupHelper.DistinctIds(records, r => r.CompanyId),
            cancellationToken);

        var departmentNames = await RiskLookupHelper.LoadDepartmentNamesAsync(
            departmentRepository,
            RiskLookupHelper.DistinctIds(records, r => r.DepartmentId),
            cancellationToken);

        var lineCounts = await LoadLineCountsAsync(records, cancellationToken);

        foreach (var item in items)
        {
            item.CompanyName = companyNames.GetValueOrDefault(item.CompanyId);
            item.DepartmentName = item.DepartmentId is { } departmentId
                ? departmentNames.GetValueOrDefault(departmentId)
                : null;
            item.LineCount = lineCounts.GetValueOrDefault(item.Id);
        }

        return new PagedResultDto<FieldObservationReportListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<FieldObservationReportDto> CreateAsync(
        CreateFieldObservationReportDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.FieldObservation.Create);

        var report = ObjectMapper.Map<CreateFieldObservationReportDto, FieldObservationReport>(input);

        report = await reportRepository.InsertAsync(report, autoSave: true, cancellationToken);

        if (input.SendMail)
        {
            // Legacy [NotMapped] MailGonder / MailAddress: a post-save notification request,
            // not report data. Recorded here for the notification pipeline to pick up.
            Logger.LogInformation(
                "Field observation report {ReportId} requested a notification mail to {MailAddress}.",
                report.Id, input.MailAddress);
        }

        return ObjectMapper.Map<FieldObservationReport, FieldObservationReportDto>(report);
    }

    /// <inheritdoc />
    public async Task<FieldObservationReportDto> UpdateAsync(
        int id,
        UpdateFieldObservationReportDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.FieldObservation.Update);

        var report = await reportRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(FieldObservationReport), id);

        ObjectMapper.Map(input, report);

        report = await reportRepository.UpdateAsync(report, autoSave: true, cancellationToken);

        return ObjectMapper.Map<FieldObservationReport, FieldObservationReportDto>(report);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.FieldObservation.Delete);

        var report = await reportRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(FieldObservationReport), id);

        var lines = await reportRepository.GetLinesAsync(id, cancellationToken);

        // A line that already produced a corrective action carries an open obligation; the
        // whole report may not be removed while any of them is still standing.
        foreach (var line in lines)
        {
            await EnsureNoCorrectiveActionAsync(line.Id, cancellationToken);
        }

        await lineRepository.DeleteManyAsync(lines, autoSave: false, cancellationToken);
        await reportRepository.DeleteAsync(report, autoSave: true, cancellationToken);

        Logger.LogInformation("Field observation report deleted: {ReportId}", id);
    }

    // ------------------------------------------------------------------- Lines

    /// <inheritdoc />
    public async Task<ListResultDto<FieldObservationLineDto>> GetLinesAsync(
        int reportId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.FieldObservation.Default);

        _ = await reportRepository.FindAsync(reportId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(FieldObservationReport), reportId);

        var lines = await reportRepository.GetLinesAsync(reportId, cancellationToken);
        var reference = Clock.Now.Date;

        return new ListResultDto<FieldObservationLineDto>(
            [.. lines.Select(l => MapLine(l, reference))]);
    }

    /// <inheritdoc />
    public async Task<FieldObservationLineDto> AddLineAsync(
        int reportId,
        CreateFieldObservationLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.FieldObservation.Update);

        var report = await reportRepository.FindAsync(reportId, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(FieldObservationReport), reportId);

        var line = ObjectMapper.Map<CreateFieldObservationLineDto, FieldObservationLine>(input);
        line.FieldObservationReportId = reportId;
        line.Date ??= report.Date;

        line = await lineRepository.InsertAsync(line, autoSave: true, cancellationToken);

        return MapLine(line, Clock.Now.Date);
    }

    /// <inheritdoc />
    public async Task<FieldObservationLineDto> UpdateLineAsync(
        int reportId,
        int lineId,
        UpdateFieldObservationLineDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.FieldObservation.Update);

        var line = await FindLineInReportAsync(reportId, lineId, cancellationToken);

        ObjectMapper.Map(input, line);
        line.FieldObservationReportId = reportId;

        line = await lineRepository.UpdateAsync(line, autoSave: true, cancellationToken);

        return MapLine(line, Clock.Now.Date);
    }

    /// <inheritdoc />
    public async Task RemoveLineAsync(
        int reportId,
        int lineId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.FieldObservation.Update);

        var line = await FindLineInReportAsync(reportId, lineId, cancellationToken);

        await EnsureNoCorrectiveActionAsync(lineId, cancellationToken);

        await lineRepository.DeleteAsync(line, autoSave: true, cancellationToken);
    }

    // ----------------------------------------------------------------- Helpers

    private async Task<FieldObservationLine> FindLineInReportAsync(
        int reportId,
        int lineId,
        CancellationToken cancellationToken)
    {
        var line = await lineRepository.FindAsync(lineId, cancellationToken)
                   ?? throw new EntityNotFoundException(typeof(FieldObservationLine), lineId);

        if (line.FieldObservationReportId != reportId)
        {
            throw new BusinessException(
                    "The observation line does not belong to the given field observation report.",
                    "Ensa:FieldObservation:LineNotInReport")
                .WithData("LineId", lineId)
                .WithData("ReportId", reportId);
        }

        return line;
    }

    /// <summary>Refuses removal while corrective actions still point at the line.</summary>
    private async Task EnsureNoCorrectiveActionAsync(int lineId, CancellationToken cancellationToken)
    {
        var derived = await correctiveActionRepository.GetByFieldObservationLineAsync(lineId, cancellationToken);

        if (derived.Count > 0)
        {
            throw new BusinessException(
                    "This observation line cannot be removed because corrective actions were derived from it.",
                    "Ensa:FieldObservation:LineHasCorrectiveAction")
                .WithData("LineId", lineId)
                .WithData("CorrectiveActionCount", derived.Count);
        }
    }

    private FieldObservationLineDto MapLine(FieldObservationLine line, DateTime reference)
    {
        var dto = ObjectMapper.Map<FieldObservationLine, FieldObservationLineDto>(line);
        dto.IsOverdue = line.DeadlineDate is { } deadline && deadline.Date < reference;
        return dto;
    }

    /// <summary>
    /// Line counts for a whole page in one query. The repository exposes no aggregate helper and
    /// this layer has no EF Core reference (so no <c>GroupBy</c> + <c>CountAsync</c>); the rows
    /// are fetched once with an <c>IN (...)</c> filter and grouped in memory.
    /// </summary>
    private async Task<Dictionary<int, int>> LoadLineCountsAsync(
        List<FieldObservationReport> records,
        CancellationToken cancellationToken)
    {
        var reportIds = records.ConvertAll(r => r.Id);
        if (reportIds.Count == 0)
        {
            return [];
        }

        var lines = await lineRepository.GetListAsync(
            l => reportIds.Contains(l.FieldObservationReportId), cancellationToken);

        var counts = new Dictionary<int, int>(reportIds.Count);
        foreach (var line in lines)
        {
            counts[line.FieldObservationReportId] = counts.GetValueOrDefault(line.FieldObservationReportId) + 1;
        }

        return counts;
    }

    private static Expression<Func<FieldObservationReport, bool>>? BuildFilter(
        GetFieldObservationReportListInput input)
    {
        var filter = new RiskFilter<FieldObservationReport>();

        filter.AddIf(input.CompanyId is { }, r => r.CompanyId == input.CompanyId!.Value);
        filter.AddIf(input.DepartmentId is { }, r => r.DepartmentId == input.DepartmentId!.Value);
        if (input.DateFrom is { } from)
        {
            filter.Add(r => r.Date >= from);
        }

        if (input.DateTo is { } to)
        {
            filter.Add(r => r.Date <= to);
        }


        return filter.Build();
    }
}
