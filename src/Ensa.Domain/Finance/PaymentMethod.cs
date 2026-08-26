using Ensa.Domain.Common;

namespace Ensa.Domain.Finance;

/// <summary>
/// A payment or collection method: cash, credit card, bank transfer and so on.
/// <para>Legacy equivalent: <c>PaymentMethod_T</c>.</para>
/// <para>A host reference table with no tenant — shared by every tenant.</para>
/// </summary>
public class PaymentMethod : AuditedEntity, IActivatable
{
    /// <summary>(Legacy: <c>OdemeTuru</c>)</summary>
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
