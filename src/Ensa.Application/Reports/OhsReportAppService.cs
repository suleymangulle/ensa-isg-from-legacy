using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Reports;
using Ensa.Application.Contracts.Reports.Dtos;
using Ensa.Domain.Reports;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Application.Reports;

/// <summary>
/// OHS service-time report application service.
/// <para>
/// <b>Read-only, and this service generates nothing.</b> These records are produced by the
/// reporting engine from assignment and work-plan data; computing them is out of scope here,
/// which is why there is deliberately no create, update or delete path.
/// </para>
/// </summary>
public class OhsReportAppService(
    IServiceProvider serviceProvider,
    IOhsReportRepository ohsReportRepository)
    : EnsaAppService(serviceProvider), IOhsReportAppService
{
    /// <inheritdoc />
    public async Task<OhsReportDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Default);

        var report = await ohsReportRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(OhsReport), id);

        return ObjectMapper.Map<OhsReport, OhsReportDto>(report);
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<OhsReportListDto>> GetListAsync(
        GetOhsReportListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Report.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "CreationTime DESC");

        var total = await ohsReportRepository.GetCountAsync(predicate, cancellationToken);

        var records = await ohsReportRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<OhsReport>, List<OhsReportListDto>>(records);

        return new PagedResultDto<OhsReportListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<OhsReportDto>> GetOfficeReportsAsync(
        int officeId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Default);

        if (from is { } start && to is { } end && start > end)
        {
            throw new BusinessException(
                "The start of the reporting period must not be later than its end.",
                "Ensa:Report:InvalidPeriod");
        }

        var records = await ohsReportRepository.GetOfficeReportsAsync(officeId, from, to, cancellationToken);

        return new ListResultDto<OhsReportDto>(
            ObjectMapper.Map<List<OhsReport>, List<OhsReportDto>>(records));
    }

    /// <inheritdoc />
    public async Task<ListResultDto<OhsReportHazardClassBreakdownDto>> GetHazardClassBreakdownAsync(
        int reportId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Report.Default);

        _ = await ohsReportRepository.FindAsync(reportId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(OhsReport), reportId);

        var breakdown = await ohsReportRepository.GetHazardClassBreakdownAsync(reportId, cancellationToken);

        // Every real hazard class is returned, with zero where the report has no row, so a chart
        // does not have to infer which buckets are missing. "Unspecified" is left out: it is a
        // data-entry placeholder, not a class a workplace can actually be in.
        var items = new List<OhsReportHazardClassBreakdownDto>
        {
            BuildBucket(breakdown, HazardClass.LowHazard),
            BuildBucket(breakdown, HazardClass.Hazardous),
            BuildBucket(breakdown, HazardClass.VeryHazardous)
        };

        return new ListResultDto<OhsReportHazardClassBreakdownDto>(items);
    }

    // -----------------------------------------------------------------

    private static OhsReportHazardClassBreakdownDto BuildBucket(
        Dictionary<HazardClass, int> breakdown,
        HazardClass hazardClass)
        => new()
        {
            HazardClass = hazardClass,
            CompanyCount = breakdown.TryGetValue(hazardClass, out var count) ? count : 0
        };

    private static Expression<Func<OhsReport, bool>>? BuildFilter(GetOhsReportListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var officeId = input.OfficeId;
        var staffRole = input.StaffRole;
        var dutyType = input.DutyType;
        var startDate = input.StartDate;
        var endDate = input.EndDate;

        if (search is null
            && officeId is null
            && staffRole is null
            && dutyType is null
            && startDate is null
            && endDate is null)
        {
            return null;
        }

        return r =>
            (search == null || r.EmployeeName.Contains(search) || r.NationalId.Contains(search))
            && (officeId == null || r.OfficeId == officeId)
            && (staffRole == null || r.StaffRole == staffRole)
            && (dutyType == null || r.DutyType == dutyType)
            && (startDate == null || r.CreationTime >= startDate)
            && (endDate == null || r.CreationTime <= endDate);
    }
}
