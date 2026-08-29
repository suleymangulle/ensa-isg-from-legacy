using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Communication;
using Ensa.Application.Contracts.Communication.Dtos;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Communication;
using Ensa.Domain.Companies;
using Ensa.Domain.Tenancy;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Ensa.Domain.Membership;
using Ensa.Domain.Repositories;


namespace Ensa.Application.Communication;

/// <summary>
/// Visit application service — the visits and appointments a specialist or physician plans and
/// carries out at a workplace.
/// </summary>
public class VisitAppService(
    IServiceProvider serviceProvider,
    IVisitRepository visitRepository,
    IUserRepository userRepository,
    ICompanyRepository companyRepository,
    IReadOnlyRepository<UserProfile> userProfileRepository)
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

        var predicate = BuildFilter(input, ScopedCompanyIds());
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
        visit.IsCompleted = false;

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

        var records = await visitRepository.GetCalendarAsync(
            userId,
            from,
            to,
            ResolveOfficeScope(requestedOfficeId: null).OfficeIds,
            cancellationToken);

        // The colour and the name a calendar entry falls back to are on the profile, so they are
        // fetched once for everyone on the calendar rather than per entry.
        var visitUserIds = records.Select(n => n.Visit.UserId).Distinct().ToList();

        var names = await userRepository.GetDisplaysAsync(visitUserIds, cancellationToken);

        var colours = (await userProfileRepository.GetListAsync(
                p => visitUserIds.Contains(p.UserId), cancellationToken))
            .ToDictionary(p => p.UserId, p => p.Color);

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
                    Color = string.IsNullOrWhiteSpace(visit.Color)
                        ? colours.GetValueOrDefault(visit.UserId)
                        : visit.Color,
                    CompanyId = visit.CompanyId,
                    CompanyName = n.Company?.CompanyName,
                    UserId = visit.UserId,
                    UserFullName = names.TryGetValue(visit.UserId, out var who)
                        ? who.DisplayName
                        : null,
                    OperationType = visit.OperationType,
                    IsCompleted = visit.IsCompleted
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

    /// <summary>
    /// The workplaces the request's office context covers, as a composable subquery, or <c>null</c>
    /// when there is no office restriction.
    /// <para>
    /// A visit carries no office of its own: it is the workplace that belongs to an office
    /// (<c>Company.OfficeId</c>), and the legacy visit calendar scoped itself by exactly that join
    /// (<c>ZiyaretTakvimiController</c>: <c>f.OfisId == OfisId</c>). Returned as an
    /// <see cref="IQueryable{T}"/> rather than a materialised id list so it stays one round trip
    /// however many workplaces an office has.
    /// </para>
    /// </summary>
    private IQueryable<int>? ScopedCompanyIds()
    {
        var officeIds = ResolveOfficeScope(requestedOfficeId: null).OfficeIds;

        return officeIds.Count == 0
            ? null
            : companyRepository.GetReadOnlyQueryable()
                .Where(f => officeIds.Contains(f.OfficeId))
                .Select(f => f.Id);
    }

    /// <summary>
    /// The list filter.
    /// <para>
    /// The office restriction is <b>appended</b> rather than written into the body as
    /// <c>scopedCompanyIds == null || scopedCompanyIds.Contains(...)</c>, and that is not a style
    /// choice. The other optional terms close over scalars, which EF turns into query parameters
    /// and can leave a redundant comparison in the SQL; a subquery is not a value, so comparing one
    /// against <c>null</c> inside the tree is a translation failure — the whole list endpoint
    /// answers 500 for anyone who has an office context, and nobody without one ever sees it.
    /// </para>
    /// </summary>
    private static Expression<Func<Visit, bool>>? BuildFilter(
        GetVisitListInput input,
        IQueryable<int>? scopedCompanyIds)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var companyId = input.CompanyId;
        var userId = input.UserId;
        var operationType = input.OperationType;
        var completed = input.IsCompleted;
        var startDate = input.StartDate;
        var endDate = input.EndDate;

        var filtered = search is not null
                       || companyId is not null
                       || userId is not null
                       || operationType is not null
                       || completed is not null
                       || startDate is not null
                       || endDate is not null;

        if (!filtered && scopedCompanyIds is null)
        {
            return null;
        }

        Expression<Func<Visit, bool>> predicate = z =>
            (search == null || (z.Description != null && z.Description.Contains(search)))
            && (companyId == null || z.CompanyId == companyId)
            && (userId == null || z.UserId == userId)
            && (operationType == null || z.OperationType == operationType)
            && (completed == null || z.IsCompleted == completed)
            && (startDate == null || z.VisitDate >= startDate)
            && (endDate == null || z.VisitDate <= endDate);

        if (scopedCompanyIds is not { } scoped)
        {
            return predicate;
        }

        Expression<Func<Visit, bool>> officeScope = z => scoped.Contains(z.CompanyId);

        return filtered ? Combine(predicate, officeScope) : officeScope;
    }

    /// <summary>
    /// Logical AND of two predicates over the same entity, rebound onto one parameter — two lambdas
    /// written separately carry two parameter instances, and a tree holding both does not translate.
    /// Same technique as <c>CompanyAppService.Combine</c>.
    /// </summary>
    private static Expression<Func<Visit, bool>> Combine(
        Expression<Func<Visit, bool>> left,
        Expression<Func<Visit, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(Visit), "z");

        var body = Expression.AndAlso(
            new ParameterRebinder(left.Parameters[0], parameter).Visit(left.Body)!,
            new ParameterRebinder(right.Parameters[0], parameter).Visit(right.Body)!);

        return Expression.Lambda<Func<Visit, bool>>(body, parameter);
    }

    /// <summary>Rebinds the parameters of two separate lambdas onto a single shared parameter.</summary>
    private sealed class ParameterRebinder(ParameterExpression previous, ParameterExpression replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == previous ? replacement : base.VisitParameter(node);
    }
}
