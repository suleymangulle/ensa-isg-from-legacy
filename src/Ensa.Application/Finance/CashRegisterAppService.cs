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
/// Cash register application service.
/// <para>
/// <b>Why movements are append-only.</b> A cash balance is not a stored number — it is derived by
/// replaying every movement of the register, and reports, reconciliations and audits all rebuild
/// it that way. If a movement could be edited, every balance already reported would silently
/// change; if one could be deleted, the money it represented would vanish from the books with no
/// trace of who removed it. So the ledger only ever grows: <see cref="AddTransactionAsync"/>
/// appends, <see cref="VoidTransactionAsync"/> marks a row inactive so it stops counting while
/// remaining visible with its author and timestamps, and a correction is expressed as a new
/// opposite movement rather than as a rewrite. There is deliberately no update and no delete.
/// </para>
/// </summary>
public class CashRegisterAppService(
    IServiceProvider serviceProvider,
    ICashRegisterRepository cashRegisterRepository,
    IRepository<CashTransaction> cashTransactionRepository)
    : EnsaAppService(serviceProvider), ICashRegisterAppService
{
    /// <summary>Maximum number of records returned by the drop-down list.</summary>
    private const int LookupMaxRecord = 50;

    /// <inheritdoc />
    public async Task<CashRegisterDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CashRegister.Default);

        var register = await cashRegisterRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(CashRegister), id);

        return ObjectMapper.Map<CashRegister, CashRegisterDto>(register);
    }

    /// <inheritdoc />
    public async Task<CashRegisterNavigationDto> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CashRegister.Default);

        var navigation = await cashRegisterRepository.GetWithNavigationAsync(
                             id,
                             latestTransactionCount: 20,
                             cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(CashRegister), id);

        return new CashRegisterNavigationDto
        {
            CashRegister = ObjectMapper.Map<CashRegister, CashRegisterDto>(navigation.CashRegister),
            Office = navigation.Office is null
                ? null
                : new LookupDto
                {
                    Id = navigation.Office.Id,
                    DisplayName = navigation.Office.Name,
                    IsActive = navigation.Office.IsActive
                },
            Balance = navigation.Balance,
            LatestTransactions = ObjectMapper
                .Map<List<CashTransaction>, List<CashTransactionDto>>(navigation.LatestTransactions)
        };
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<CashRegisterListDto>> GetListAsync(
        GetCashRegisterListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.CashRegister.Default);

        var predicate = BuildRegisterFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "CashRegisterName ASC");

        var total = await cashRegisterRepository.GetCountAsync(predicate, cancellationToken);

        var records = await cashRegisterRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<CashRegister>, List<CashRegisterListDto>>(records);

        return new PagedResultDto<CashRegisterListDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<ListResultDto<LookupDto>> GetLookupAsync(
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CashRegister.Default);

        var search = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();

        var records = await cashRegisterRepository.GetPagedListAsync(
            skipCount: 0,
            maxResultCount: LookupMaxRecord,
            sorting: "CashRegisterName ASC",
            predicate: k => k.IsActive && (search == null || k.CashRegisterName.Contains(search)),
            cancellationToken);

        var result = records
            .Select(k => new LookupDto
            {
                Id = k.Id,
                DisplayName = k.CashRegisterName,
                IsActive = k.IsActive
            })
            .ToList();

        return new ListResultDto<LookupDto>(result);
    }

    /// <inheritdoc />
    public async Task<CashRegisterDto> CreateAsync(
        CreateCashRegisterDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.CashRegister.Create);

        var register = ObjectMapper.Map<CreateCashRegisterDto, CashRegister>(input);
        register.IsActive = true;

        register = await cashRegisterRepository.InsertAsync(register, autoSave: true, cancellationToken);

        Logger.LogInformation(
            "Cash register created: {CashRegisterId} — {CashRegisterName}",
            register.Id,
            register.CashRegisterName);

        return ObjectMapper.Map<CashRegister, CashRegisterDto>(register);
    }

    /// <inheritdoc />
    public async Task<CashRegisterDto> UpdateAsync(
        int id,
        UpdateCashRegisterDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.CashRegister.Update);

        var register = await cashRegisterRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(CashRegister), id);

        ObjectMapper.Map(input, register);

        register = await cashRegisterRepository.UpdateAsync(register, autoSave: true, cancellationToken);

        return ObjectMapper.Map<CashRegister, CashRegisterDto>(register);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CashRegister.Delete);

        var register = await cashRegisterRepository.FindAsync(id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(CashRegister), id);

        // Deleting a register that still holds money would make that money disappear from the
        // books without a movement explaining where it went.
        var balance = await cashRegisterRepository.GetBalanceAsync(id, date: null, cancellationToken);
        if (balance != 0m)
        {
            throw new BusinessException(
                    "The cash register still holds a balance and cannot be deleted.",
                    "Ensa:CashRegister:NonZeroBalance")
                .WithData("CashRegisterName", register.CashRegisterName)
                .WithData("Balance", balance);
        }

        await cashRegisterRepository.DeleteAsync(register, autoSave: true, cancellationToken);

        Logger.LogInformation("Cash register deleted: {CashRegisterId}", id);
    }

    // --------------------------------------------------------------- Balance

    /// <inheritdoc />
    public async Task<CashRegisterBalanceDto> GetBalanceAsync(
        int cashRegisterId,
        DateTime? asOf = null,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CashRegister.Default);

        var register = await cashRegisterRepository.FindAsync(cashRegisterId, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(CashRegister), cashRegisterId);

        var effectiveDate = asOf ?? Clock.Now;

        var balance = await cashRegisterRepository.GetBalanceAsync(
            cashRegisterId,
            effectiveDate,
            cancellationToken);

        return new CashRegisterBalanceDto
        {
            CashRegisterId = cashRegisterId,
            CashRegisterName = register.CashRegisterName,
            Balance = balance,
            AsOf = effectiveDate
        };
    }

    // ---------------------------------------------------------- Transactions

    /// <inheritdoc />
    public async Task<PagedResultDto<CashTransactionDto>> GetTransactionListAsync(
        GetCashTransactionListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.CashRegister.Default);

        _ = await cashRegisterRepository.FindAsync(input.CashRegisterId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(CashRegister), input.CashRegisterId);

        var predicate = BuildTransactionFilter(input);
        var sorting = NormalizeSorting(input.Sorting, "OperationDate DESC");

        var total = await cashTransactionRepository.GetCountAsync(predicate, cancellationToken);

        var records = await cashTransactionRepository.GetPagedListAsync(
            input.SkipCount,
            input.MaxResultCount,
            sorting,
            predicate,
            cancellationToken);

        var items = ObjectMapper.Map<List<CashTransaction>, List<CashTransactionDto>>(records);

        return new PagedResultDto<CashTransactionDto>(total, items);
    }

    /// <inheritdoc />
    public async Task<CashTransactionDto> AddTransactionAsync(
        CreateCashTransactionDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await CheckPermissionAsync(EnsaPermissions.CashRegister.Create);

        var register = await cashRegisterRepository.FindAsync(input.CashRegisterId, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(CashRegister), input.CashRegisterId);

        if (!register.IsActive)
        {
            throw new BusinessException(
                    "The cash register is not active and cannot accept new movements.",
                    "Ensa:CashRegister:InactiveRegister")
                .WithData("CashRegisterName", register.CashRegisterName);
        }

        if (input.OperationType == CashTransactionType.Outflow)
        {
            await EnsureBalanceStaysNonNegativeAsync(register, input.OperationAmount, cancellationToken);
        }

        var transaction = ObjectMapper.Map<CreateCashTransactionDto, CashTransaction>(input);
        transaction.OperationDate = input.OperationDate ?? Clock.Now;
        transaction.IsActive = true;

        transaction = await cashTransactionRepository.InsertAsync(
            transaction,
            autoSave: true,
            cancellationToken);

        Logger.LogInformation(
            "Cash movement added: {TransactionId} — register {CashRegisterId}, {OperationType} {Amount}",
            transaction.Id,
            transaction.CashRegisterId,
            transaction.OperationType,
            transaction.OperationAmount);

        return ObjectMapper.Map<CashTransaction, CashTransactionDto>(transaction);
    }

    /// <inheritdoc />
    public async Task<CashTransactionDto> VoidTransactionAsync(
        int transactionId,
        CancellationToken cancellationToken = default)
    {
        await CheckPermissionAsync(EnsaPermissions.CashRegister.Update);

        var transaction = await cashTransactionRepository.FindAsync(transactionId, cancellationToken)
                          ?? throw new EntityNotFoundException(typeof(CashTransaction), transactionId);

        if (!transaction.IsActive)
        {
            throw new BusinessException(
                    "The cash movement has already been voided.",
                    "Ensa:CashRegister:TransactionAlreadyVoided")
                .WithData("TransactionId", transactionId);
        }

        var register = await cashRegisterRepository.FindAsync(transaction.CashRegisterId, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(CashRegister), transaction.CashRegisterId);

        // Voiding an entry withdraws money that has already been counted, so it is subject to the
        // same non-negative rule as an exit.
        if (transaction.OperationType == CashTransactionType.Inflow)
        {
            await EnsureBalanceStaysNonNegativeAsync(register, transaction.OperationAmount, cancellationToken);
        }

        // The row is kept on purpose — see the append-only rationale on this class.
        transaction.IsActive = false;

        transaction = await cashTransactionRepository.UpdateAsync(
            transaction,
            autoSave: true,
            cancellationToken);

        Logger.LogInformation("Cash movement voided: {TransactionId}", transactionId);

        return ObjectMapper.Map<CashTransaction, CashTransactionDto>(transaction);
    }

    // -----------------------------------------------------------------

    /// <summary>
    /// Refuses an outflow that would drive the register below zero — a physical cash box cannot
    /// hold a negative amount, and a negative balance in the ledger always means a missing entry.
    /// </summary>
    private async Task EnsureBalanceStaysNonNegativeAsync(
        CashRegister register,
        decimal outflow,
        CancellationToken cancellationToken)
    {
        var balance = await cashRegisterRepository.GetBalanceAsync(
            register.Id,
            date: null,
            cancellationToken);

        if (balance - outflow < 0m)
        {
            throw new BusinessException(
                    "The cash register balance would go negative.",
                    "Ensa:CashRegister:NegativeBalance")
                .WithData("CashRegisterName", register.CashRegisterName)
                .WithData("Balance", balance)
                .WithData("Amount", outflow);
        }
    }

    private static Expression<Func<CashRegister, bool>>? BuildRegisterFilter(GetCashRegisterListInput input)
    {
        var search = string.IsNullOrWhiteSpace(input.Filter) ? null : input.Filter.Trim();
        var officeId = input.OfficeId;
        var headquarter = input.HeadquarterCashRegister;
        var isActive = input.IsActive;

        if (search is null && officeId is null && headquarter is null && isActive is null)
        {
            return null;
        }

        return k =>
            (search == null || k.CashRegisterName.Contains(search))
            && (officeId == null || k.OfficeId == officeId)
            && (headquarter == null || k.HeadquarterCashRegister == headquarter)
            && (isActive == null || k.IsActive == isActive);
    }

    private static Expression<Func<CashTransaction, bool>> BuildTransactionFilter(
        GetCashTransactionListInput input)
    {
        var cashRegisterId = input.CashRegisterId;
        var operationType = input.OperationType;
        var sourceModule = input.SourceModule;
        var startDate = input.StartDate;
        var endDate = input.EndDate;
        var includeVoided = input.IncludeVoided;

        return h =>
            h.CashRegisterId == cashRegisterId
            && (includeVoided || h.IsActive)
            && (operationType == null || h.OperationType == operationType)
            && (sourceModule == null || h.SourceModule == sourceModule)
            && (startDate == null || (h.OperationDate != null && h.OperationDate >= startDate))
            && (endDate == null || (h.OperationDate != null && h.OperationDate <= endDate));
    }
}
