using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication;
using Ensa.Application.Contracts.Communication.Dtos;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Communication;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Communication;

/// <summary>
/// Visit application service — the visits and appointments a specialist or physician plans and
/// carries out at a workplace.
/// </summary>
public class VisitAppService(
    IServiceProvider serviceProvider,
    IVisitRepository visitRepository)
    : EnsaAppService(serviceProvider), IVisitAppService
{
    /// <summary>
    /// Widest date range the calendar endpoint accepts. A calendar screen never needs more than a
    /// year at a time, and without a ceiling a single request could ask the database to join
    /// every visit the organization has ever recorded.
    /// </summary>
    private const int CalendarMaximumDayCount = 366;

    /// <inheritdoc />
    public async Task<VisitDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Visit.Default);

        var visit = await visitRepository.FindAsync(id, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Visit), id);

        return ObjectMapper.Map<Visit, VisitDto>(visit);
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<VisitListDto>> GetListAsync(
        GetVisitListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Visit.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "VisitDate DESC");

        var total = await visitRepository.GetCountAsync(predicate, cancellationToken);

        var records = await visitRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Visit>, List<VisitListDto>>(records);

        return new PagedResultDto<VisitListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<VisitDto> CreateAsync(
        CreateVisitDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Visit.Create);

        EnsureTimeRangeValid(input.Start, input.End);

        var visit = ObjectMapper.Map<CreateVisitDto, Visit>(input);

        // A visit recorded without an explicit owner belongs to whoever recorded it.
        visit.UserId = input.UserId ?? GetRequiredUserId();
        visit.Completed = false;

        visit = await visitRepository.InsertAsync(visit, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Visit created: {VisitId} — workplace {CompanyId}, user {UserId}",
            visit.Id,
            visit.CompanyId,
            visit.UserId);

        return ObjectMapper.Map<Visit, VisitDto>(visit);
    }

    /// <inheritdoc />
    public async Task<VisitDto> UpdateAsync(
        int id,
        UpdateVisitDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Visit.Update);

        EnsureTimeRangeValid(input.Start, input.End);

        var visit = await visitRepository.FindAsync(id, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Visit), id);

        var previousUserId = visit.UserId;

        ObjectMapper.Map(input, visit);

        visit.UserId = input.UserId ?? previousUserId;

        visit = await visitRepository.UpdateAsync(visit, autoSave: true, cancellationToken);

        return ObjectMapper.Map<Visit, VisitDto>(visit);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Visit.Delete);

        var visit = await visitRepository.FindAsync(id, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Visit), id);

        await visitRepository.DeleteAsync(visit, autoSave: true, cancellationToken);

        Logger.LogInformation("Visit deleted: {VisitId}", id);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<VisitCalendarDto>> GetCalendarAsync(
        int? userId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Visit.Default);

        if (from > to)
        {
            throw new BusinessException(
                "The start of the range must not be later than its end.",
                "Ensa:Visit:InvalidDateRange");
        }

        if ((to - from).TotalDays > CalendarMaximumDayCount)
        {
            throw new BusinessException(
                    "The requested calendar range is too wide.",
                    "Ensa:Visit:CalendarRangeTooWide")
                .WithData("MaximumDayCount", CalendarMaximumDayCount);
        }

        var records = await visitRepository.GetCalendarAsync(userId, from, to, cancellationToken);

        var items = records
            .Select(n =>
            {
                var visit = n.Visit;
                var start = visit.Start ?? visit.VisitDate;
                var end = visit.End ?? start;

                return new VisitCalendarDto
                {
                    Id = visit.Id,
                    Title = string.IsNullOrWhiteSpace(visit.Description)
                        ? n.Company?.CompanyName ?? string.Empty
                        : visit.Description,
                    Start = start,
                    End = end,
                    // Falling back to the user's own colour keeps one person's entries visually
                    // grouped even when individual visits were saved without a colour.
                    Color = string.IsNullOrWhiteSpace(visit.Color) ? n.User?.Color : visit.Color,
                    CompanyId = visit.CompanyId,
                    CompanyName = n.Company?.CompanyName,
                    UserId = visit.UserId,
                    UserFullName = n.User?.FullName,
                    OperationType = visit.OperationType,
                    Completed = visit.Completed
                };
            })
            .OrderBy(v => v.Start)
            .ToList();

        return new ListResultDto<VisitCalendarDto>(items);
    }

    // -----------------------------------------------------------------

    private static void EnsureTimeRangeValid(DateTime? start, DateTime? end)
    {
        if (start is { } s && end is { } e && s > e)
        {
            throw new BusinessException(
                "The start of the range must not be later than its end.",
                "Ensa:Visit:InvalidDateRange");
        }
    }

    private static Expression<Func<Visit, bool>>? BuildFilter(GetVisitListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var companyId = input.CompanyId;
        var userId = input.UserId;
        var operationType = input.OperationType;
        var completed = input.Completed;
        var startDate = input.StartDate;
        var endDate = input.EndDate;

        if (search is null
            && companyId is null
            && userId is null
            && operationType is null
            && completed is null
            && startDate is null
            && endDate is null)
        {
            return null;
        }

        return z =>
            (search == null || (z.Description != null && z.Description.Contains(search)))
            && (companyId == null || z.CompanyId == companyId)
            && (userId == null || z.UserId == userId)
            && (operationType == null || z.OperationType == operationType)
            && (completed == null || z.Completed == completed)
            && (startDate == null || z.VisitDate >= startDate)
            && (endDate == null || z.VisitDate <= endDate);
    }
}
