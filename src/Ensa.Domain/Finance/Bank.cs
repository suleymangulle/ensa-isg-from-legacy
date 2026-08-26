using Ensa.Domain.Common;

namespace Ensa.Domain.Finance;

/// <summary>
/// A bank account the organization uses for collections. Customers raise a <see cref="Payment"/>
/// notification for the money they transfer into it.
/// <para>Legacy equivalent: <c>Bank_T</c>.</para>
/// <para>
/// CAUTION: the legacy table had NO <c>OrganizationId</c> column — a gap left over from the
/// single-tenant era. Since every organization needs its own collection accounts, this entity
/// deliberately implements <c>IMultiTenant</c>.
/// </para>
/// </summary>
public class Bank : FullAuditedTenantEntity, IActivatable
{
    public string BankName { get; set; } = string.Empty;

    public string Iban { get; set; } = string.Empty;

    /// <summary>Account holder or payee name.</summary>
    public string Recipient { get; set; } = string.Empty;

    public string? BranchName { get; set; }

    public string? AccountNo { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Bank logo or image — FK to the central <c>Document</c> table.
    /// (Legacy: <c>byte[] BankaGorseli</c>)
    /// </summary>
    public int? ImageDocumentId { get; set; }
}
