using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Finance;

/// <summary>
/// A bank payment reported by the customer — a collection notification awaiting confirmation.
/// <para>Legacy equivalent: <c>Odemeler_T</c> (file <c>Payment_T.cs</c>; the legacy PK was <c>Odemeler</c> → <c>Id</c>).</para>
/// </summary>
public class Payment : AuditedTenantEntity
{
    /// <summary>The bank account the payment was made into. FK — no navigation property.</summary>
    public int BankId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>(Legacy: <c>Durum</c> string)</summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime NotificationDate { get; set; }

    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// Payment receipt — FK to the central <c>Document</c> table. (Legacy: <c>MakbuzId</c>)
    /// </summary>
    public int? ReceiptDocumentId { get; set; }
}
