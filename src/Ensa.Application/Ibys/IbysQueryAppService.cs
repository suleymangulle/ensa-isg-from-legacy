using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Ibys;
using Ensa.Application.Contracts.Ibys.Dtos;
using Ensa.Application.Contracts.Ibys.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Companies;
using Ensa.Domain.Ibys;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Ibys;

/// <summary>
/// IBYS (İSG Bilgi Yönetim Sistemi) submission tracking application service.
/// <para>
/// <b>SECURITY.</b> Three values stay inside the domain and are never returned by this
/// service in any shape:
/// <list type="bullet">
/// <item><c>IbysQuery.XmlData</c> — the notification payload, which carries the employee's
/// clinical examination data and is held as an encrypted column.</item>
/// <item><c>IbysQuery.SignedData</c> — the CAdES envelope produced with the corporate
/// e-signature; releasing it would hand out a reusable signed artefact.</item>
/// <item><c>ESignatureLicense.License</c> — the signing component's licence key, a secret.</item>
/// </list>
/// Operators are given <c>HasXmlData</c> / <c>HasSignedData</c> flags instead, which is
/// enough to reason about where a submission stands. The payloads are read only by the
/// background submission worker, straight from the repository.
/// </para>
/// <para>
/// Status changes are validated by <see cref="IIbysSubmissionManager"/>. That manager
/// validates only — it performs no persistence — so this service saves the entity itself.
/// </para>
/// </summary>
public class IbysQueryAppService(
    IServiceProvider serviceProvider,
    IIbysQueryRepository queryRepository,
    IIbysSubmissionManager submissionManager,
    IReadOnlyRepository<Company> companyRepository,
    IReadOnlyRepository<CompanyEmployee> employeeRepository)
    : EnsaAppService(serviceProvider), IIbysQueryAppService
{
    /// <summary>Upper bound for the pending-submission queue.</summary>
    private const int PendingAbsoluteMaxResultCount = 500;

    /// <inheritdoc />
    public async Task<IbysQueryDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Ibys.Default);

        var query = await queryRepository.FindAsync(id, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(IbysQuery), id);

        return ObjectMapper.Map<IbysQuery, IbysQueryDto>(query);
    }

    /// <inheritdoc />
    public async Task<IbysQueryNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Ibys.Default);

        // One repository call returns the submission, the workplace, the employee and the
        // attached examination forms — the forms are not fetched one by one.
        var navigation = await queryRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(IbysQuery), id);

        return new IbysQueryNavigationDto
        {
            Query = ObjectMapper.Map<IbysQuery, IbysQueryDto>(navigation.Query),
            Company = navigation.Company is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Company.Id,
                    DisplayName = navigation.Company.CompanyName,
                    Code = navigation.Company.SsiNumber,
                    IsActive = navigation.Company.IsActive
                },
            Employee = navigation.Employee is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Employee.Id,
                    DisplayName = $"{navigation.Employee.Name} {navigation.Employee.LastName}".Trim(),
                    IsActive = navigation.Employee.IsActive
                },
            ApproverFullName = navigation.ApproverFullName,
            // Attached forms are reduced to their submission envelope: a tracking screen
            // must not double as a window into health records.
            ExaminationForms =
            [
                .. navigation.ExaminationForms.Select(f => new IbysSubmittedFormDto
                {
                    Id = f.Id,
                    CompanyEmployeeId = f.CompanyEmployeeId,
                    ReportType = f.ReportType,
                    ExaminationDate = f.ExaminationDate,
                    IbysStatus = f.IbysStatus,
                    IbysStatusCode = f.IbysStatusCode,
                    IbysStatusMessage = f.IbysStatusMessage
                })
            ]
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<IbysQueryListDto>> GetListAsync(
        GetIbysQueryListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Ibys.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "SubmissionDate DESC");

        var total = await queryRepository.GetCountAsync(predicate, cancellationToken);

        var records = await queryRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = await ToListDtosAsync(records, cancellationToken);

        return new PagedResultDto<IbysQueryListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<IbysQueryListDto>> GetPendingAsync(
        IbysQueryType type,
        int maxResultCount = 100,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Ibys.Default);

        var limit = maxResultCount <= 0 ? 100 : Math.Min(maxResultCount, PendingAbsoluteMaxResultCount);

        var records = await queryRepository.GetPendingAsync(type, limit, cancellationToken);

        var items = await ToListDtosAsync(records, cancellationToken);

        return new ListResultDto<IbysQueryListDto>(items);
    }

    /// <inheritdoc />
    public async Task<IbysQueryDto> UpdateStatusAsync(
        int id,
        IbysSubmissionStatus status,
        string? message,
        string? submissionNumber,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Ibys.Update);

        var query = await queryRepository.FindAsync(id, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(IbysQuery), id);

        // The state machine belongs to the manager; an invalid edge throws from there.
        submissionManager.ValidateStatusTransition(query.Status, status);

        query.Status = status;

        if (!string.IsNullOrWhiteSpace(message))
        {
            query.IbysMessage = message.Trim();
        }

        if (!string.IsNullOrWhiteSpace(submissionNumber))
        {
            query.QueryNo = submissionNumber.Trim();
        }

        if (status == IbysSubmissionStatus.Sent)
        {
            query.SubmissionDate = Clock.Now;
        }

        // ValidateStatusTransition only validates; nothing was saved by the manager, so the
        // entity is persisted here — exactly once.
        query = await queryRepository.UpdateAsync(query, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "IBYS submission status changed. QueryId={QueryId}, Status={Status}", id, status);

        return ObjectMapper.Map<IbysQuery, IbysQueryDto>(query);
    }

    // -----------------------------------------------------------------

    /// <summary>
    /// Projects submissions to list rows, resolving workplace and employee names with one
    /// batched query each — never one query per row.
    /// </summary>
    private async Task<List<IbysQueryListDto>> ToListDtosAsync(
        List<IbysQuery> records,
        CancellationToken cancellationToken)
    {
        var items = ObjectMapper.Map<List<IbysQuery>, List<IbysQueryListDto>>(records);

        if (items.Count == 0)
        {
            return items;
        }

        var companyIds = records.Where(q => q.CompanyId.HasValue).Select(q => q.CompanyId!.Value).Distinct().ToList();
        var employeeIds = records
            .Where(q => q.CompanyEmployeeId.HasValue)
            .Select(q => q.CompanyEmployeeId!.Value)
            .Distinct()
            .ToList();

        List<Company> companies = companyIds.Count == 0
            ? []
            : await companyRepository.GetListAsync(c => companyIds.Contains(c.Id), cancellationToken);

        List<CompanyEmployee> employees = employeeIds.Count == 0
            ? []
            : await employeeRepository.GetListAsync(e => employeeIds.Contains(e.Id), cancellationToken);

        var companyNames = companies.ToDictionary(c => c.Id, c => c.CompanyName);
        var employeeNames = employees.ToDictionary(e => e.Id, e => $"{e.Name} {e.LastName}".Trim());

        foreach (var item in items)
        {
            if (item.CompanyId is { } companyId && companyNames.TryGetValue(companyId, out var companyName))
            {
                item.CompanyName = companyName;
            }

            if (item.CompanyEmployeeId is { } employeeId && employeeNames.TryGetValue(employeeId, out var name))
            {
                item.EmployeeFullName = name;
            }
        }

        return items;
    }

    private static Expression<Func<IbysQuery, bool>>? BuildFilter(GetIbysQueryListInput input)
    {
        Expression<Func<IbysQuery, bool>> predicate = q => true;
        var applied = false;

        if (input.QueryType is { } queryType)
        {
            predicate = Combine(predicate, q => q.QueryType == queryType);
            applied = true;
        }

        if (input.Status is { } status)
        {
            predicate = Combine(predicate, q => q.Status == status);
            applied = true;
        }

        if (input.CompanyId is { } companyId)
        {
            predicate = Combine(predicate, q => q.CompanyId == companyId);
            applied = true;
        }

        if (input.CompanyEmployeeId is { } employeeId)
        {
            predicate = Combine(predicate, q => q.CompanyEmployeeId == employeeId);
            applied = true;
        }

        if (!string.IsNullOrWhiteSpace(input.GroupId))
        {
            var groupId = input.GroupId.Trim();
            predicate = Combine(predicate, q => q.GroupId == groupId);
            applied = true;
        }

        if (input.SubmissionDateFrom is { } from)
        {
            predicate = Combine(predicate, q => q.SubmissionDate >= from);
            applied = true;
        }

        if (input.SubmissionDateTo is { } to)
        {
            predicate = Combine(predicate, q => q.SubmissionDate <= to);
            applied = true;
        }

        // Free-text search covers the submission envelope only — never the XML payload.
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var search = input.Filter.Trim();
            predicate = Combine(predicate, q =>
                (q.QueryNo != null && q.QueryNo.Contains(search))
                || (q.GroupId != null && q.GroupId.Contains(search))
                || (q.IbysMessage != null && q.IbysMessage.Contains(search)));
            applied = true;
        }

        return applied ? predicate : null;
    }

    private static Expression<Func<IbysQuery, bool>> Combine(
        Expression<Func<IbysQuery, bool>> left,
        Expression<Func<IbysQuery, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(IbysQuery), "q");

        var body = Expression.AndAlso(
            new ParameterRebinder(left.Parameters[0], parameter).Visit(left.Body)!,
            new ParameterRebinder(right.Parameters[0], parameter).Visit(right.Body)!);

        return Expression.Lambda<Func<IbysQuery, bool>>(body, parameter);
    }

    /// <summary>Rewrites two separate lambdas onto a single shared parameter.</summary>
    private sealed class ParameterRebinder(ParameterExpression previous, ParameterExpression replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == previous ? replacement : base.VisitParameter(node);
    }
}
