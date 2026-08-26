using System.Linq.Expressions;
using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Finance;
using Ensa.Application.Contracts.Finance.Dtos;
using Ensa.Application.Contracts.Finance.Dtos.Navigations;
using Ensa.Application.Contracts.Permissions;
using Ensa.Domain.Finance;
using Ensa.Domain.Repositories;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ensa.Application.Finance;

/// <summary>
/// Statutory fine catalogue application service.
/// <para>
/// These records are host data: the fines defined by Law 6331 are identical for every
/// organization, so a single catalogue is shared and only administrators maintain it. The
/// amounts are held in a normalized matrix keyed by hazard class, head-count band and year,
/// which is what allows a fine assessed in an earlier year to be reproduced after the annual
/// revaluation has moved the current figures.
/// </para>
/// </summary>
public class PenaltyAppService(
    IServiceProvider serviceProvider,
    IPenaltyRepository penaltyRepository,
    IRepository<PenaltyAmount> penaltyAmountRepository)
    : EnsaAppService(serviceProvider), IPenaltyAppService
{
    /// <inheritdoc />
    public async Task<PenaltyDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Penalty.Default);

        var penalty = await penaltyRepository.FindAsync(id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Penalty), id);

        return ObjectMapper.Map<Penalty, PenaltyDto>(penalty);
    }

    /// <inheritdoc />
    public async Task<PenaltyNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Penalty.Default);

        var navigation = await penaltyRepository.GetWithNavigationAsync(id, cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(Penalty), id);

        return new PenaltyNavigationDto
        {
            Penalty = ObjectMapper.Map<Penalty, PenaltyDto>(navigation.Penalty),
            Amounts =
            [
                .. ObjectMapper
                    .Map<List<PenaltyAmount>, List<PenaltyAmountDto>>(navigation.Amounts)
                    .OrderByDescending(t => t.ValidityYear)
                    .ThenBy(t => t.HazardClass)
                    .ThenBy(t => t.EmployeeCountRange)
            ]
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<PenaltyListDto>> GetListAsync(
        GetPenaltyListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Penalty.Default);

        var predicate = BuildFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "LawArticle ASC");

        var total = await penaltyRepository.GetCountAsync(predicate, cancellationToken);

        var records = await penaltyRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<Penalty>, List<PenaltyListDto>>(records);

        return new PagedResultDto<PenaltyListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<PenaltyDto> CreateAsync(
        CreatePenaltyDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Penalty.Create);

        var penalty = ObjectMapper.Map<CreatePenaltyDto, Penalty>(input);
        penalty.IsActive = true;

        penalty = await penaltyRepository.InsertAsync(penalty, autoSave: true, cancellationToken);

        Logger.LogInformation("Fine article created: {PenaltyId} — {LawArticle}", penalty.Id, penalty.LawArticle);

        return ObjectMapper.Map<Penalty, PenaltyDto>(penalty);
    }

    /// <inheritdoc />
    public async Task<PenaltyDto> UpdateAsync(
        int id,
        UpdatePenaltyDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Penalty.Update);

        var penalty = await penaltyRepository.FindAsync(id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Penalty), id);

        ObjectMapper.Map(input, penalty);

        penalty = await penaltyRepository.UpdateAsync(penalty, autoSave: true, cancellationToken);

        return ObjectMapper.Map<Penalty, PenaltyDto>(penalty);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Penalty.Delete);

        var penalty = await penaltyRepository.FindAsync(id, cancellationToken)
                      ?? throw new EntityNotFoundException(typeof(Penalty), id);

        var amounts = await penaltyAmountRepository.GetListAsync(t => t.PenaltyId == id, cancellationToken);
        if (amounts.Count > 0)
        {
            await penaltyAmountRepository.DeleteManyAsync(amounts, autoSave: false, cancellationToken);
        }

        await penaltyRepository.DeleteAsync(penalty, autoSave: true, cancellationToken);

        Logger.LogInformation("Fine article deleted: {PenaltyId}", id);
    }

    // --------------------------------------------------------- Amount matrix

    /// <inheritdoc />
    public async Task<ListResultDto<PenaltyAmountDto>> GetAmountsAsync(
        int penaltyId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Penalty.Default);

        _ = await penaltyRepository.FindAsync(penaltyId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Penalty), penaltyId);

        var amounts = await penaltyAmountRepository.GetListAsync(
            t => t.PenaltyId == penaltyId,
            cancellationToken);

        var items = ObjectMapper
            .Map<List<PenaltyAmount>, List<PenaltyAmountDto>>(amounts)
            .OrderByDescending(t => t.ValidityYear)
            .ThenBy(t => t.HazardClass)
            .ThenBy(t => t.EmployeeCountRange)
            .ToList();

        return new ListResultDto<PenaltyAmountDto>(items);
    }

    /// <inheritdoc />
    public async Task<PenaltyAmountDto> AddAmountAsync(
        int penaltyId,
        CreatePenaltyAmountDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Penalty.Create);

        _ = await penaltyRepository.FindAsync(penaltyId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Penalty), penaltyId);

        await EnsureAmountCellFreeAsync(penaltyId, input, exceptAmountId: null, cancellationToken);

        var amount = ObjectMapper.Map<CreatePenaltyAmountDto, PenaltyAmount>(input);
        amount.PenaltyId = penaltyId;

        amount = await penaltyAmountRepository.InsertAsync(amount, autoSave: true, cancellationToken);

        return ObjectMapper.Map<PenaltyAmount, PenaltyAmountDto>(amount);
    }

    /// <inheritdoc />
    public async Task<PenaltyAmountDto> UpdateAmountAsync(
        int penaltyId,
        int amountId,
        UpdatePenaltyAmountDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.Penalty.Update);

        _ = await penaltyRepository.FindAsync(penaltyId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Penalty), penaltyId);

        var amount = await GetAmountOfPenaltyAsync(penaltyId, amountId, cancellationToken);

        await EnsureAmountCellFreeAsync(penaltyId, input, amountId, cancellationToken);

        ObjectMapper.Map(input, amount);
        amount.PenaltyId = penaltyId;

        amount = await penaltyAmountRepository.UpdateAsync(amount, autoSave: true, cancellationToken);

        return ObjectMapper.Map<PenaltyAmount, PenaltyAmountDto>(amount);
    }

    /// <inheritdoc />
    public async Task RemoveAmountAsync(
        int penaltyId,
        int amountId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Penalty.Delete);

        var amount = await GetAmountOfPenaltyAsync(penaltyId, amountId, cancellationToken);

        await penaltyAmountRepository.DeleteAsync(amount, autoSave: true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApplicablePenaltyAmountDto> GetApplicableAmountAsync(
        int penaltyId,
        HazardClass hazardClass,
        EmployeeCountRange range,
        int year,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Penalty.Default);

        _ = await penaltyRepository.FindAsync(penaltyId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Penalty), penaltyId);

        // The repository falls back to the closest earlier year when the requested one has no
        // row, so a schedule that has not yet been revalued still yields a usable figure.
        var amount = await penaltyRepository.GetAmountAsync(
                         penaltyId,
                         hazardClass,
                         range,
                         year,
                         cancellationToken)
                     ?? throw new BusinessException(
                             "No fine amount is defined for this hazard class, head-count band and year.",
                             "Ensa:Penalty:AmountNotDefined")
                         .WithData("HazardClass", hazardClass)
                         .WithData("EmployeeCountRange", range)
                         .WithData("Year", year);

        return new ApplicablePenaltyAmountDto
        {
            PenaltyId = penaltyId,
            HazardClass = hazardClass,
            EmployeeCountRange = range,
            Year = year,
            Amount = amount
        };
    }

    // -----------------------------------------------------------------

    private async Task<PenaltyAmount> GetAmountOfPenaltyAsync(
        int penaltyId,
        int amountId,
        CancellationToken cancellationToken)
    {
        var amount = await penaltyAmountRepository.FindAsync(amountId, cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(PenaltyAmount), amountId);

        if (amount.PenaltyId != penaltyId)
        {
            throw new EntityNotFoundException(typeof(PenaltyAmount), amountId);
        }

        return amount;
    }

    /// <summary>
    /// Guards the (article, hazard class, head-count band, year) uniqueness. Two rows for the
    /// same cell would make the resolved amount depend on row order, which is how a fine could
    /// silently change value between two reads.
    /// </summary>
    private async Task EnsureAmountCellFreeAsync(
        int penaltyId,
        CreatePenaltyAmountDto input,
        int? exceptAmountId,
        CancellationToken cancellationToken)
    {
        var hazardClass = input.HazardClass;
        var range = input.EmployeeCountRange;
        var year = input.ValidityYear;

        var exists = await penaltyAmountRepository.AnyAsync(
            t => t.PenaltyId == penaltyId
                 && t.HazardClass == hazardClass
                 && t.EmployeeCountRange == range
                 && t.ValidityYear == year
                 && (exceptAmountId == null || t.Id != exceptAmountId),
            cancellationToken);

        if (exists)
        {
            throw new BusinessException(
                    "An amount is already defined for this hazard class, head-count band and year.",
                    "Ensa:Penalty:AmountAlreadyDefined")
                .WithData("HazardClass", hazardClass)
                .WithData("EmployeeCountRange", range)
                .WithData("Year", year);
        }
    }

    private static Expression<Func<Penalty, bool>>? BuildFilter(GetPenaltyListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var isActive = input.IsActive;
        var multiplier = input.MultiplierCalculate;

        if (search is null && isActive is null && multiplier is null)
        {
            return null;
        }

        return c =>
            (search == null
             || c.LawArticle.Contains(search)
             || c.PenaltyArticle.Contains(search)
             || (c.TreeNodeCode != null && c.TreeNodeCode.Contains(search)))
            && (isActive == null || c.IsActive == isActive)
            && (multiplier == null || c.MultiplierCalculate == multiplier);
    }
}
