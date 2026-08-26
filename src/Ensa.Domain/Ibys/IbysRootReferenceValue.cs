using Ensa.Domain.Common;

namespace Ensa.Domain.Ibys;

/// <summary>
/// IBYS parent (root) reference value — the grouping node for dependent reference values.
/// <para>Legacy equivalent: <c>IBYSParentReferenceDegerler_T</c>.</para>
/// <para>
/// <b>NOTE:</b> the legacy class carried NO <c>[Key]</c> attribute (EF discovered the key by
/// naming convention alone); here <c>Id</c> is declared as the explicit primary key.
/// </para>
/// <para>Host reference table — does NOT implement <c>IMultiTenant</c>.</para>
/// </summary>
public class IbysRootReferenceValue : AuditedEntity, IActivatable
{
    /// <summary>Reference code defined by IBYS. (Legacy: <c>Kod</c>)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name of the reference. (Legacy: <c>ReferansAdi</c>)</summary>
    public string ReferenceName { get; set; } = string.Empty;

    /// <summary>Whether the reference is still in use. (Legacy: <c>AktifMi</c>)</summary>
    public bool IsActive { get; set; } = true;
}
