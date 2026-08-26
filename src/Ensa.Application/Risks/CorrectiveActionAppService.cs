using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Permissions;
using Ensa.Application.Contracts.Risks;
using Ensa.Application.Contracts.Risks.Dtos;
using Ensa.Application.Contracts.Risks.Dtos.Navigations;
using Ensa.Domain.Companies;
using Ensa.Domain.Repositories;
using Ensa.Domain.Risks;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Risks;

/// <summary>
/// Corrective / preventive action (DOF) application service.
/// <para>
/// The Risks module has no <c>CorrectiveActionManager</c>, so the closing rules live here and
/// are surfaced as localized <see cref="BusinessException"/> codes.
/// </para>
/// </summary>
public class CorrectiveActionAppService(
    IServiceProvider serviceProvider,
    ICorrectiveActionRepository correctiveActionRepository,
    IReadOnlyRepository<Company> companyRepository)
    : EnsaAppService(serviceProvider), ICorrectiveActionAppService
{
    /// <inheritdoc />
    public async Task<CorrectiveActionDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CorrectiveAction.Default);

        var action = await correctiveActionRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(CorrectiveAction), id);

        return MapAction(action);
    }

    /// <inheritdoc />
    public async Task<CorrectiveActionNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CorrectiveAction.Default);

        // Single repository call joins company, owner, both documents and the source line.
        var navigation = await correctiveActionRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(CorrectiveAction), id);

        return new CorrectiveActionNavigationDto
        {
            CorrectiveAction = MapAction(navigation.CorrectiveAction),
            Company = RiskLookupHelper.Lookup(navigation.Company?.Id, navigation.Company?.CompanyName),
            OwnerEmployee = RiskLookupHelper.Lookup(
                navigation.OwnerEmployee?.Id,
                navigation.OwnerEmployee is { } employee ? $"{employee.Name} {employee.LastName}".Trim() : null),
            FindingDocument = RiskLookupHelper.Lookup(
                navigation.FindingDocument?.Id, navigation.FindingDocument?.DocumentName),
            ResultDocument = RiskLookupHelper.Lookup(
                navigation.ResultDocument?.Id, navigation.ResultDocument?.DocumentName),
            SourceFieldObservationLine = navigation.SourceFieldObservationLine is { } line
                ? ObjectMapper.Map<FieldObservationLine, FieldObservationLineDto>(line)
                : null
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<CorrectiveActionListDto>> GetListAsync(
        GetCorrectiveActionListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.CorrectiveAction.Default);

        var reference = Clock.Now.Date;
        var predicate = BuildFilter(input, reference);
        var sorting = NormalizeSorting(input.Sorting, "FindingDate DESC");

        var total = await correctiveActionRepository.GetCountAsync(predicate, cancellationToken);

        var records = await correctiveActionRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = await MapListAsync(records, reference, cancellationToken);

        return new PagedResultDto<CorrectiveActionListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<CorrectiveActionDto> CreateAsync(
        CreateCorrectiveActionDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.CorrectiveAction.Create);

        var action = ObjectMapper.Map<CreateCorrectiveActionDto, CorrectiveAction>(input);
        action.OperationResult = CorrectiveActionStatus.InProgress;
        action.FindingDate ??= Clock.Now.Date;

        action = await correctiveActionRepository.InsertAsync(action, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Corrective action created: {CorrectiveActionId} (Company: {CompanyId})",
            action.Id, action.CompanyId);

        return MapAction(action);
    }

    /// <inheritdoc />
    public async Task<CorrectiveActionDto> UpdateAsync(
        int id,
        UpdateCorrectiveActionDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.CorrectiveAction.Update);

        var action = await correctiveActionRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(CorrectiveAction), id);

        // Closing data is owned by CloseAsync; the mapping leaves Result / ResultDate /
        // OperationResult untouched so an edit cannot silently reopen or re-close the record.
        ObjectMapper.Map(input, action);

        action = await correctiveActionRepository.UpdateAsync(action, autoSave: true, cancellationToken);

        return MapAction(action);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CorrectiveAction.Delete);

        var action = await correctiveActionRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(CorrectiveAction), id);

        await correctiveActionRepository.DeleteAsync(action, autoSave: true, cancellationToken);

        Logger.LogInformation("Corrective action deleted: {CorrectiveActionId}", id);
    }

    /// <inheritdoc />
    public async Task<int> GetOpenCountAsync(int companyId, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CorrectiveAction.Default);

        return await correctiveActionRepository.GetOpenCorrectiveActionCountAsync(companyId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<CorrectiveActionListDto>> GetOverdueAsync(
        int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CorrectiveAction.Default);

        var reference = Clock.Now.Date;

        var records = await correctiveActionRepository.GetDeadlineOverdueAsync(
            reference, companyId, cancellationToken);

        var items = await MapListAsync(records, reference, cancellationToken);

        return new ListResultDto<CorrectiveActionListDto>(items);
    }

    /// <inheritdoc />
    public async Task<CorrectiveActionDto> CloseAsync(
        int id,
        string result,
        DateTime resultDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(result);
        await CheckPermissionAsync(EnsaPermissions.CorrectiveAction.Approve);

        var action = await correctiveActionRepository.FindAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(CorrectiveAction), id);

        if (action.OperationResult != CorrectiveActionStatus.InProgress)
        {
            throw new BusinessException(
                    "This corrective action is no longer open, so it cannot be closed.",
                    "Ensa:CorrectiveAction:AlreadyClosed")
                .WithData("CorrectiveActionId", id)
                .WithData("Status", action.OperationResult);
        }

        if (action.FindingDate is { } findingDate && resultDate.Date < findingDate.Date)
        {
            throw new BusinessException(
                    "The result date cannot be earlier than the finding date.",
                    "Ensa:CorrectiveAction:ResultDateBeforeFindingDate")
                .WithData("ResultDate", resultDate)
                .WithData("FindingDate", findingDate);
        }

        action.Result = result;
        action.ResultDate = resultDate;
        action.OperationResult = CorrectiveActionStatus.Closed;

        action = await correctiveActionRepository.UpdateAsync(action, autoSave: true, cancellationToken);

        Logger.LogInformation("Corrective action closed: {CorrectiveActionId}", id);

        return MapAction(action);
    }

    // ------------------------------------------------------------------ Helpers

    private CorrectiveActionDto MapAction(CorrectiveAction action)
    {
        var dto = ObjectMapper.Map<CorrectiveAction, CorrectiveActionDto>(action);
        dto.IsOverdue = IsOverdue(action, Clock.Now.Date);
        return dto;
    }

    /// <summary>
    /// Maps a page of actions and fills <c>CompanyName</c> with one batched company query
    /// rather than a lookup per row.
    /// </summary>
    private async Task<List<CorrectiveActionListDto>> MapListAsync(
        List<CorrectiveAction> records,
        DateTime reference,
        CancellationToken cancellationToken)
    {
        var items = ObjectMapper.Map<List<CorrectiveAction>, List<CorrectiveActionListDto>>(records);

        var companyNames = await RiskLookupHelper.LoadCompanyNamesAsync(
            companyRepository,
            RiskLookupHelper.DistinctIds(records, a => a.CompanyId),
            cancellationToken);

        for (var i = 0; i < items.Count; i++)
        {
            items[i].CompanyName = companyNames.GetValueOrDefault(items[i].CompanyId);
            items[i].IsOverdue = IsOverdue(records[i], reference);
        }

        return items;
    }

    private static bool IsOverdue(CorrectiveAction action, DateTime reference)
        => action.OperationResult == CorrectiveActionStatus.InProgress
           && action.DeadlineDate is { } deadline
           && deadline.Date < reference;

    private static Expression<Func<CorrectiveAction, bool>>? BuildFilter(
        GetCorrectiveActionListInput input,
        DateTime reference)
    {
        var filter = new RiskFilter<CorrectiveAction>();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var search = input.Filter.Trim();
            filter.Add(a =>
                a.Finding.Contains(search)
                || (a.Recommendation != null && a.Recommendation.Contains(search))
                || (a.Owner != null && a.Owner.Contains(search))
                || (a.Source != null && a.Source.Contains(search)));
        }

        filter.AddIf(input.CompanyId is { }, a => a.CompanyId == input.CompanyId!.Value);
        filter.AddIf(input.OperationResult is { }, a => a.OperationResult == input.OperationResult!.Value);
        filter.AddIf(input.RiskCategory is { }, a => a.RiskCategory == input.RiskCategory!.Value);
        filter.AddIf(
            input.OwnerCompanyEmployeeId is { },
            a => a.OwnerCompanyEmployeeId == input.OwnerCompanyEmployeeId!.Value);
        filter.AddIf(
            input.FieldObservationLineId is { },
            a => a.FieldObservationLineId == input.FieldObservationLineId!.Value);
        if (input.FindingFrom is { } from)
        {
            filter.Add(a => a.FindingDate >= from);
        }

        if (input.FindingTo is { } to)
        {
            filter.Add(a => a.FindingDate <= to);
        }


        filter.AddIf(
            input.OnlyOverdue,
            a => a.OperationResult == CorrectiveActionStatus.InProgress
                 && a.DeadlineDate != null
                 && a.DeadlineDate < reference);

        return filter.Build();
    }
}
