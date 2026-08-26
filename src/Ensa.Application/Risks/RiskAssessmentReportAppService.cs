using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Risks;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Repositories;
using Ensa.Domain.Risks;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Risks;

/// <summary>
/// Risk assessment report application service — the largest aggregate of the Risks module.
/// <para>
/// Notes for maintainers:
/// <list type="bullet">
/// <item>Risk scores, risk levels and validity dates come from <see cref="IRiskAssessmentManager"/>.
/// They are never recomputed here.</item>
/// <item><b>This manager does not persist.</b> Unlike <c>CompanyManager</c>, every
/// <see cref="IRiskAssessmentManager"/> method is a pure calculation (or a read), so the
/// service is responsible for calling <c>InsertAsync</c>/<c>UpdateAsync</c> itself.</item>
/// <item>No <c>try/catch</c> — <c>EnsaExceptionFilter</c> wraps failures at the HTTP boundary.</item>
/// <item>Tenant isolation comes from the global query filter; no manual <c>TenantId</c> checks.</item>
/// </list>
/// </para>
/// </summary>
public class RiskAssessmentReportAppService(
    IServiceProvider serviceProvider,
    IRiskAssessmentReportRepository reportRepository,
    IRepository<IdentifiedHazard> hazardRepository,
    IRepository<ControlMeasure> controlMeasureRepository,
    IRepository<RiskAssessmentExposedGroup> exposedGroupRepository,
    IRepository<RiskAssessmentControlMeasure> headerControlMeasureRepository,
    IRepository<RiskAssessmentImprovementAction> improvementActionRepository,
    IRepository<RiskAssessmentProtectedGroup> protectedGroupRepository,
    IRepository<RiskAssessmentParticipant> participantRepository,
    IRepository<RiskAssessmentHistoryRecord> historyRecordRepository,
    IReadOnlyRepository<Company> companyRepository,
    IRiskAssessmentManager riskAssessmentManager)
    : EnsaAppService(serviceProvider), IRiskAssessmentReportAppService
{
    /// <inheritdoc />
    public async Task<RiskAssessmentReportDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Default);

        var report = await reportRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(RiskAssessmentReport), id);

        return MapReport(report);
    }

    /// <inheritdoc />
    public async Task<RiskAssessmentReportNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Default);

        // One repository call returns the report and every child collection already joined,
        // so the projection below touches no further tables (no N+1 over hazards/measures).
        var navigation = await reportRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(RiskAssessmentReport), id);

        var method = navigation.Report.ReportMethod;

        var dto = new RiskAssessmentReportNavigationDto
        {
            Report = MapReport(navigation.Report),
            Company = RiskLookupHelper.Lookup(navigation.Company?.Id, navigation.Company?.CompanyName),
            Specialist = RiskLookupHelper.Lookup(
                navigation.Specialist?.Id,
                FullName(navigation.Specialist?.Name, navigation.Specialist?.LastName)),
            Physician = RiskLookupHelper.Lookup(
                navigation.Physician?.Id,
                FullName(navigation.Physician?.Name, navigation.Physician?.LastName)),
            IdentifiedHazards =
            [
                .. navigation.IdentifiedHazards.Select(h => new IdentifiedHazardNavigationDto
                {
                    IdentifiedHazard = MapHazard(h.IdentifiedHazard, method),
                    Category = RiskLookupHelper.Lookup(h.Category?.Id, h.Category?.CategoryName),
                    LibraryHazard = RiskLookupHelper.Lookup(h.LibraryHazard?.Id, h.LibraryHazard?.HazardTag),
                    ControlMeasures = ObjectMapper.Map<List<ControlMeasure>, List<ControlMeasureDto>>(h.ControlMeasures)
                })
            ],
            ExposedGroups = ObjectMapper
                .Map<List<RiskAssessmentExposedGroup>, List<RiskAssessmentExposedGroupDto>>(navigation.ExposedGroups),
            ControlMeasures = ObjectMapper
                .Map<List<RiskAssessmentControlMeasure>, List<RiskAssessmentControlMeasureDto>>(navigation.ProtectionMeasures),
            ImprovementActions = ObjectMapper
                .Map<List<RiskAssessmentImprovementAction>, List<RiskAssessmentImprovementActionDto>>(navigation.ImprovementActions),
            ProtectedGroups = ObjectMapper
                .Map<List<RiskAssessmentProtectedGroup>, List<RiskAssessmentProtectedGroupDto>>(navigation.SpecialGroups),
            Participants = ObjectMapper
                .Map<List<RiskAssessmentParticipant>, List<RiskAssessmentParticipantDto>>(navigation.Participants),
            HistoryRecords = ObjectMapper
                .Map<List<RiskAssessmentHistoryRecord>, List<RiskAssessmentHistoryRecordDto>>(navigation.HistoryRecords)
        };

        // Counted from the already-materialised hazard list instead of a second query.
        dto.OpenHighRiskHazardCount = dto.IdentifiedHazards.Count(h =>
            h.IdentifiedHazard.ResidualRiskScore is null
                ? h.IdentifiedHazard.RiskLevel >= RiskLevel.High
                : h.IdentifiedHazard.ResidualRiskLevel >= RiskLevel.High);

        return dto;
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<RiskAssessmentReportListDto>> GetListAsync(
        GetRiskAssessmentReportListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Default);

        var reference = Clock.Now.Date;
        var predicate = BuildFilter(input, reference);
        var sorting = NormalizeSorting(input.Sorting, "PerformedDate DESC");

        var total = await reportRepository.GetCountAsync(predicate, cancellationToken);

        var records = await reportRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = await MapListAsync(records, reference, cancellationToken);

        return new PagedResultDto<RiskAssessmentReportListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<RiskAssessmentReportDto> CreateAsync(
        CreateRiskAssessmentReportDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Create);

        var report = ObjectMapper.Map<CreateRiskAssessmentReportDto, RiskAssessmentReport>(input);

        // Renewal interval (2 / 4 / 6 years by hazard class) is a legal rule owned by the manager.
        report.ValidityDate = riskAssessmentManager.CalculateValidUntilDate(input.PerformedDate, input.HazardClass);
        report.ApprovalStatus = ApprovalStatus.Draft;

        // IRiskAssessmentManager performs no persistence, so the service saves the aggregate root.
        report = await reportRepository.InsertAsync(report, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Risk assessment report created: {ReportId} — {ReportName} (Company: {CompanyId})",
            report.Id, report.ReportName, report.CompanyId);

        return MapReport(report);
    }

    /// <inheritdoc />
    public async Task<RiskAssessmentReportDto> UpdateAsync(
        int id,
        UpdateRiskAssessmentReportDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Update);

        var report = await reportRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(RiskAssessmentReport), id);

        ObjectMapper.Map(input, report);

        report.ValidityDate = riskAssessmentManager.CalculateValidUntilDate(input.PerformedDate, input.HazardClass);

        report = await reportRepository.UpdateAsync(report, autoSave: true, cancellationToken);

        return MapReport(report);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Delete);

        var report = await reportRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(RiskAssessmentReport), id);

        // Children are only reachable through the report, so they are removed with it;
        // otherwise the rows would linger and pollute cross-report queries.
        var hazards = await hazardRepository.GetListAsync(h => h.RiskAssessmentReportId == id, cancellationToken);

        if (hazards.Count > 0)
        {
            var hazardIds = hazards.ConvertAll(h => h.Id);
            var measures = await controlMeasureRepository.GetListAsync(
                m => hazardIds.Contains(m.IdentifiedHazardId), cancellationToken);

            await controlMeasureRepository.DeleteManyAsync(measures, autoSave: false, cancellationToken);
            await hazardRepository.DeleteManyAsync(hazards, autoSave: false, cancellationToken);
        }

        await exposedGroupRepository.DeleteDirectAsync(x => x.RiskAssessmentReportId == id, cancellationToken);
        await headerControlMeasureRepository.DeleteDirectAsync(x => x.RiskAssessmentReportId == id, cancellationToken);
        await improvementActionRepository.DeleteDirectAsync(x => x.RiskAssessmentReportId == id, cancellationToken);
        await protectedGroupRepository.DeleteDirectAsync(x => x.RiskAssessmentReportId == id, cancellationToken);
        await participantRepository.DeleteDirectAsync(x => x.RiskAssessmentReportId == id, cancellationToken);

        var historyRecords = await historyRecordRepository.GetListAsync(
            x => x.RiskAssessmentReportId == id, cancellationToken);
        await historyRecordRepository.DeleteManyAsync(historyRecords, autoSave: false, cancellationToken);

        await reportRepository.DeleteAsync(report, autoSave: true, cancellationToken);

        Logger.LogInformation("Risk assessment report deleted: {ReportId}", id);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<RiskAssessmentReportListDto>> GetExpiringAsync(
        DateTime asOf,
        int withinDays = 30,
        int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Default);

        var records = await reportRepository.GetDurationExpiredAsync(
            asOf,
            Math.Max(withinDays, 0),
            companyId,
            cancellationToken);

        var items = await MapListAsync(records, asOf.Date, cancellationToken);

        return new ListResultDto<RiskAssessmentReportListDto>(items);
    }

    /// <inheritdoc />
    public async Task<RiskAssessmentReportDto?> GetActiveForCompanyAsync(
        int companyId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Default);

        var report = await reportRepository.GetActiveReportAsync(companyId, Clock.Now, cancellationToken);

        return report is null ? null : MapReport(report);
    }

    // ------------------------------------------------------- Identified hazards

    /// <inheritdoc />
    public async Task<IdentifiedHazardDto> AddIdentifiedHazardAsync(
        int reportId,
        CreateIdentifiedHazardDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Update);

        var report = await reportRepository.FindAsync(reportId, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(RiskAssessmentReport), reportId);

        EnsureReportEditable(report);
        EnsureReportNotExpired(report);

        var hazard = ObjectMapper.Map<CreateIdentifiedHazardDto, IdentifiedHazard>(input);
        hazard.RiskAssessmentReportId = reportId;

        // Scores the line against the report method and writes RiskScore / ResidualRiskScore.
        // The manager only calculates; the insert below is the single persistence point.
        await riskAssessmentManager.CalculateAsync(hazard, cancellationToken);

        hazard = await hazardRepository.InsertAsync(hazard, autoSave: true, cancellationToken);

        return MapHazard(hazard, report.ReportMethod);
    }

    /// <inheritdoc />
    public async Task<IdentifiedHazardDto> UpdateIdentifiedHazardAsync(
        int reportId,
        int hazardId,
        UpdateIdentifiedHazardDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Update);

        var report = await reportRepository.FindAsync(reportId, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(RiskAssessmentReport), reportId);

        EnsureReportEditable(report);

        var hazard = await FindHazardInReportAsync(reportId, hazardId, cancellationToken);

        ObjectMapper.Map(input, hazard);
        hazard.RiskAssessmentReportId = reportId;

        await riskAssessmentManager.CalculateAsync(hazard, cancellationToken);

        hazard = await hazardRepository.UpdateAsync(hazard, autoSave: true, cancellationToken);

        return MapHazard(hazard, report.ReportMethod);
    }

    /// <inheritdoc />
    public async Task RemoveIdentifiedHazardAsync(
        int reportId,
        int hazardId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Update);

        var report = await reportRepository.FindAsync(reportId, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(RiskAssessmentReport), reportId);

        EnsureReportEditable(report);

        var hazard = await FindHazardInReportAsync(reportId, hazardId, cancellationToken);

        var measures = await controlMeasureRepository.GetListAsync(
            m => m.IdentifiedHazardId == hazardId, cancellationToken);

        await controlMeasureRepository.DeleteManyAsync(measures, autoSave: false, cancellationToken);
        await hazardRepository.DeleteAsync(hazard, autoSave: true, cancellationToken);
    }

    // -------------------------------------------------- Hazard control measures

    /// <inheritdoc />
    public async Task<ControlMeasureDto> AddControlMeasureAsync(
        int hazardId,
        CreateControlMeasureDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Update);

        var hazard = await hazardRepository.FindAsync(hazardId, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(IdentifiedHazard), hazardId);

        var report = await reportRepository.FindAsync(hazard.RiskAssessmentReportId, cancellationToken)
                     ?? throw new EntityNotFoundException(
                         typeof(RiskAssessmentReport), hazard.RiskAssessmentReportId);

        EnsureReportEditable(report);
        EnsureReportNotExpired(report);

        var measure = ObjectMapper.Map<CreateControlMeasureDto, ControlMeasure>(input);
        measure.IdentifiedHazardId = hazardId;

        measure = await controlMeasureRepository.InsertAsync(measure, autoSave: true, cancellationToken);

        return ObjectMapper.Map<ControlMeasure, ControlMeasureDto>(measure);
    }

    /// <inheritdoc />
    public async Task<ControlMeasureDto> CompleteControlMeasureAsync(
        int controlMeasureId,
        DateTime completionDate,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Update);

        var measure = await controlMeasureRepository.FindAsync(controlMeasureId, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(ControlMeasure), controlMeasureId);

        if (measure.IsCompleted)
        {
            throw new BusinessException(
                    "The control measure has already been completed.",
                    "Ensa:RiskAssessment:ControlMeasureAlreadyCompleted")
                .WithData("Measure", measure.Measure)
                .WithData("CompletionDate", measure.CompletionDate);
        }

        measure.IsCompleted = true;
        measure.CompletionDate = completionDate;

        measure = await controlMeasureRepository.UpdateAsync(measure, autoSave: true, cancellationToken);

        return ObjectMapper.Map<ControlMeasure, ControlMeasureDto>(measure);
    }

    // ------------------------------------------------- Header checkbox groups

    /// <inheritdoc />
    public async Task<ListResultDto<RiskAssessmentExposedGroupDto>> SetExposedGroupsAsync(
        int reportId,
        List<ExposedPersonGroup> groups,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Update);

        var records = await ReplaceHeaderSelectionAsync(
            reportId,
            groups,
            exposedGroupRepository,
            x => x.RiskAssessmentReportId == reportId,
            x => x.Group,
            value => new RiskAssessmentExposedGroup { RiskAssessmentReportId = reportId, Group = value },
            cancellationToken);

        return new ListResultDto<RiskAssessmentExposedGroupDto>(
            ObjectMapper.Map<List<RiskAssessmentExposedGroup>, List<RiskAssessmentExposedGroupDto>>(records));
    }

    /// <inheritdoc />
    public async Task<ListResultDto<RiskAssessmentControlMeasureDto>> SetExistingControlMeasuresAsync(
        int reportId,
        List<ExistingControlMeasure> measures,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Update);

        var records = await ReplaceHeaderSelectionAsync(
            reportId,
            measures,
            headerControlMeasureRepository,
            x => x.RiskAssessmentReportId == reportId,
            x => x.Measure,
            value => new RiskAssessmentControlMeasure { RiskAssessmentReportId = reportId, Measure = value },
            cancellationToken);

        return new ListResultDto<RiskAssessmentControlMeasureDto>(
            ObjectMapper.Map<List<RiskAssessmentControlMeasure>, List<RiskAssessmentControlMeasureDto>>(records));
    }

    /// <inheritdoc />
    public async Task<ListResultDto<RiskAssessmentImprovementActionDto>> SetImprovementActionsAsync(
        int reportId,
        List<ImprovementAction> recommendations,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.RiskAssessment.Update);

        var records = await ReplaceHeaderSelectionAsync(
            reportId,
            recommendations,
            improvementActionRepository,
            x => x.RiskAssessmentReportId == reportId,
            x => x.Recommendation,
            value => new RiskAssessmentImprovementAction
            {
                RiskAssessmentReportId = reportId,
                Recommendation = value
            },
            cancellationToken);

        return new ListResultDto<RiskAssessmentImprovementActionDto>(
            ObjectMapper.Map<List<RiskAssessmentImprovementAction>, List<RiskAssessmentImprovementActionDto>>(records));
    }

    // ------------------------------------------------------------------ Helpers

    /// <summary>
    /// Replaces a whole header checkbox group: deletes what was unticked, inserts what is new,
    /// leaves untouched rows alone so their creation audit survives.
    /// </summary>
    private async Task<List<TEntity>> ReplaceHeaderSelectionAsync<TEntity, TValue>(
        int reportId,
        List<TValue> selection,
        IRepository<TEntity> repository,
        Expression<Func<TEntity, bool>> ownedByReport,
        Func<TEntity, TValue> valueSelector,
        Func<TValue, TEntity> factory,
        CancellationToken cancellationToken)
        where TEntity : class, IEntity<int>
        where TValue : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(selection);

        var report = await reportRepository.FindAsync(reportId, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(RiskAssessmentReport), reportId);

        EnsureReportEditable(report);

        var requested = selection.Distinct().ToList();
        var existing = await repository.GetListAsync(ownedByReport, cancellationToken);

        var removed = existing.Where(e => !requested.Contains(valueSelector(e))).ToList();
        if (removed.Count > 0)
        {
            await repository.DeleteManyAsync(removed, autoSave: false, cancellationToken);
        }

        var existingValues = existing.Select(valueSelector).ToHashSet();
        var added = requested.Where(v => !existingValues.Contains(v)).Select(factory).ToList();
        if (added.Count > 0)
        {
            await repository.InsertManyAsync(added, autoSave: false, cancellationToken);
        }

        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return await repository.GetListAsync(ownedByReport, cancellationToken);
    }

    private async Task<IdentifiedHazard> FindHazardInReportAsync(
        int reportId,
        int hazardId,
        CancellationToken cancellationToken)
    {
        var hazard = await hazardRepository.FindAsync(hazardId, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(IdentifiedHazard), hazardId);

        if (hazard.RiskAssessmentReportId != reportId)
        {
            throw new BusinessException(
                    "The hazard line does not belong to the given risk assessment report.",
                    "Ensa:RiskAssessment:HazardNotInReport")
                .WithData("HazardId", hazardId)
                .WithData("ReportId", reportId);
        }

        return hazard;
    }

    /// <summary>An approved report is a signed legal document; its content is frozen.</summary>
    private static void EnsureReportEditable(RiskAssessmentReport report)
    {
        if (report.ApprovalStatus == ApprovalStatus.Approved)
        {
            throw new BusinessException(
                    "An approved risk assessment report can no longer be modified.",
                    "Ensa:RiskAssessment:ReportApproved")
                .WithData("ReportName", report.ReportName);
        }
    }

    /// <summary>New findings may not be filed against a report whose validity has lapsed.</summary>
    private void EnsureReportNotExpired(RiskAssessmentReport report)
    {
        if (!riskAssessmentManager.IsValid(report, Clock.Now))
        {
            throw new BusinessException(
                    "The risk assessment report has expired.",
                    "Ensa:RiskAssessment:ReportExpired")
                .WithData("ValidUntil", report.ValidityDate)
                .WithData("ReportName", report.ReportName);
        }
    }

    private RiskAssessmentReportDto MapReport(RiskAssessmentReport report)
    {
        var dto = ObjectMapper.Map<RiskAssessmentReport, RiskAssessmentReportDto>(report);
        dto.IsValid = riskAssessmentManager.IsValid(report, Clock.Now);
        return dto;
    }

    private IdentifiedHazardDto MapHazard(IdentifiedHazard hazard, RiskAssessmentMethod method)
    {
        var dto = ObjectMapper.Map<IdentifiedHazard, IdentifiedHazardDto>(hazard);

        dto.RiskLevel = riskAssessmentManager.DetermineLevel(hazard.RiskScore, method);
        dto.ResidualRiskLevel = hazard.ResidualRiskScore is { } residual
            ? riskAssessmentManager.DetermineLevel(residual, method)
            : RiskLevel.Unspecified;

        return dto;
    }

    /// <summary>
    /// Maps a page of reports and fills <c>CompanyName</c> with a single batched company query
    /// instead of one lookup per row.
    /// </summary>
    private async Task<List<RiskAssessmentReportListDto>> MapListAsync(
        List<RiskAssessmentReport> records,
        DateTime reference,
        CancellationToken cancellationToken)
    {
        var items = ObjectMapper.Map<List<RiskAssessmentReport>, List<RiskAssessmentReportListDto>>(records);

        var companyNames = await RiskLookupHelper.LoadCompanyNamesAsync(
            companyRepository,
            RiskLookupHelper.DistinctIds(records, r => r.CompanyId),
            cancellationToken);

        foreach (var item in items)
        {
            item.CompanyName = companyNames.GetValueOrDefault(item.CompanyId);
            item.RemainingDays = (int)(item.ValidityDate.Date - reference).TotalDays;
            item.IsExpired = item.RemainingDays < 0;
        }

        return items;
    }

    private static Expression<Func<RiskAssessmentReport, bool>>? BuildFilter(
        GetRiskAssessmentReportListInput input,
        DateTime reference)
    {
        var filter = new RiskFilter<RiskAssessmentReport>();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var search = input.Filter.Trim();
            filter.Add(r =>
                r.ReportName.Contains(search)
                || r.WorkplaceTitle.Contains(search)
                || (r.SpecialistFullName != null && r.SpecialistFullName.Contains(search))
                || (r.PhysicianFullName != null && r.PhysicianFullName.Contains(search)));
        }

        filter.AddIf(input.CompanyId is { }, r => r.CompanyId == input.CompanyId!.Value);
        filter.AddIf(input.ApprovalStatus is { }, r => r.ApprovalStatus == input.ApprovalStatus!.Value);
        filter.AddIf(input.AssessmentMethod is { }, r => r.ReportMethod == input.AssessmentMethod!.Value);
        filter.AddIf(input.HazardClass is { }, r => r.HazardClass == input.HazardClass!.Value);
        filter.AddIf(input.SpecialistUserId is { }, r => r.SpecialistUserId == input.SpecialistUserId!.Value);
        if (input.PerformedFrom is { } from)
        {
            filter.Add(r => r.PerformedDate >= from);
        }

        if (input.PerformedTo is { } to)
        {
            filter.Add(r => r.PerformedDate <= to);
        }


        if (input.OnlyExpiringSoon)
        {
            var threshold = reference.AddDays(Math.Max(input.ExpiringWithinDays, 0));
            filter.Add(r => r.ValidityDate <= threshold);
        }

        return filter.Build();
    }

    private static string FullName(string? name, string? lastName)
        => $"{name} {lastName}".Trim();
}
