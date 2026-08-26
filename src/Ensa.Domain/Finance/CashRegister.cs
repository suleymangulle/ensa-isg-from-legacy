using Ensa.Domain.Common;

namespace Ensa.Domain.Finance;

/// <summary>
/// An office or organization cash register. Cash and collection movements live in
/// <see cref="CashTransaction"/>.
/// <para>Legacy equivalent: <c>CashRegister_T</c>.</para>
/// </summary>
public class CashRegister : FullAuditedTenantEntity, IActivatable
{
    public string CashRegisterName { get; set; } = string.Empty;

    /// <summary>The office the cash register belongs to. FK — no navigation property.</summary>
    public int OfficeId { get; set; }

    /// <summary>Whether this is the organization's headquarter cash register.</summary>
    public bool HeadquarterCashRegister { get; set; }

    /// <summary>(Not present in legacy; added so that a cash register can be deactivated.)</summary>
    public bool IsActive { get; set; } = true;
}
