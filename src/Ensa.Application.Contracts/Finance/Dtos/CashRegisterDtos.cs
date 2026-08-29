using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Finance.Dtos;

/// <summary>Cash register list row.</summary>
public class CashRegisterListDto : EntityDto
{
    public string CashRegisterName { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public bool IsHeadquarterCashRegister { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Cash register detail view.</summary>
public class CashRegisterDto : FullAuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public string CashRegisterName { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public bool IsHeadquarterCashRegister { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Cash register creation input.</summary>
public class CreateCashRegisterDto
{
    [Required(ErrorMessage = "The cash register name is required.")]
    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Name)]
    public string CashRegisterName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "An office must be selected.")]
    public int OfficeId { get; set; }

    public bool IsHeadquarterCashRegister { get; set; }
}

/// <summary>Cash register update input.</summary>
public class UpdateCashRegisterDto : CreateCashRegisterDto
{
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// A single cash movement. Transactions are an append-only ledger: they are never edited and
/// never hard-deleted, only voided (see <c>ICashRegisterAppService.VoidTransactionAsync</c>).
/// </summary>
public class CashTransactionDto : AuditedEntityDto, IMultiTenantDto
{
    public int? TenantId { get; set; }

    public int CashRegisterId { get; set; }
    public int PaymentMethodId { get; set; }
    public CashTransactionType OperationType { get; set; }
    public decimal OperationAmount { get; set; }
    public string? Description { get; set; }
    public SourceModule SourceModule { get; set; }
    public int? SourceRecordId { get; set; }
    public int? ExitItemId { get; set; }
    public DateTime? OperationDate { get; set; }

    /// <summary><c>false</c> means the movement has been voided and no longer affects the balance.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Cash movement creation input.
/// <para>There is no update counterpart on purpose — see the append-only note on
/// <see cref="CashTransactionDto"/>.</para>
/// </summary>
public class CreateCashTransactionDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A cash register must be selected.")]
    public int CashRegisterId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A payment method must be selected.")]
    public int PaymentMethodId { get; set; }

    public CashTransactionType OperationType { get; set; } = CashTransactionType.Inflow;

    [Range(0.01, 9999999999.99, ErrorMessage = "The amount must be greater than zero.")]
    public decimal OperationAmount { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Description { get; set; }

    public SourceModule SourceModule { get; set; } = SourceModule.Manual;

    public int? SourceRecordId { get; set; }

    /// <summary>Expense category — only meaningful for <see cref="CashTransactionType.Outflow"/> movements.</summary>
    public int? ExitItemId { get; set; }

    /// <summary>Value date of the movement. Defaults to now when omitted.</summary>
    public DateTime? OperationDate { get; set; }
}

/// <summary>Cash register list filter.</summary>
public class GetCashRegisterListInput : PagedAndSortedFilterDto
{
    public int? OfficeId { get; set; }
    public bool? IsHeadquarterCashRegister { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>Cash movement list filter.</summary>
public class GetCashTransactionListInput : PagedAndSortedRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A cash register must be selected.")]
    public int CashRegisterId { get; set; }

    public CashTransactionType? OperationType { get; set; }
    public SourceModule? SourceModule { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>When <c>true</c>, voided movements are included in the result.</summary>
    public bool IncludeVoided { get; set; }
}

/// <summary>Cash register balance at a point in time.</summary>
public class CashRegisterBalanceDto
{
    public int CashRegisterId { get; set; }
    public string CashRegisterName { get; set; } = string.Empty;

    /// <summary>Total of entries minus total of exits, voided movements excluded.</summary>
    public decimal Balance { get; set; }

    /// <summary>The instant the balance refers to.</summary>
    public DateTime AsOf { get; set; }
}
