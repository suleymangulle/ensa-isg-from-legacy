using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Finance;

/// <summary>
/// A cash register movement, in or out.
/// <para>Legacy equivalent: <c>CashRegisterDetail_T</c>.</para>
/// </summary>
public class CashTransaction : AuditedTenantEntity, IActivatable
{
    /// <summary>The cash register the movement belongs to. FK — no navigation property.</summary>
    public int CashRegisterId { get; set; }

    public int PaymentMethodId { get; set; }

    /// <summary>Direction of the movement. (Legacy: <c>IslemTuru</c> string)</summary>
    public CashTransactionType OperationType { get; set; }

    /// <summary>(Legacy: <c>double</c> → <c>decimal</c>)</summary>
    public decimal OperationAmount { get; set; }

    public string? Description { get; set; }

    /// <summary>The module the movement originated in. (Legacy: <c>modul</c> string)</summary>
    public SourceModule SourceModule { get; set; } = SourceModule.Unspecified;

    /// <summary>Id of the related record in the source module, e.g. Invoice.Id. (Legacy: <c>IslemId</c>) FK — no navigation property.</summary>
    public int? SourceRecordId { get; set; }

    /// <summary>Expense category, when the movement is an outflow. FK — no navigation property.</summary>
    public int? ExitItemId { get; set; }

    public DateTime? OperationDate { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
