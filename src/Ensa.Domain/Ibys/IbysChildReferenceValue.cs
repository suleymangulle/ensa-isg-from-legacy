using Ensa.Domain.Common;

namespace Ensa.Domain.Ibys;

/// <summary>
/// IBYS dependent (child) reference value.
/// <para>Legacy equivalent: <c>IBYSBagliReferenceDegerler_T</c>.</para>
/// <para>
/// <b>NOTE:</b> the legacy class carried NO <c>[Key]</c> attribute; here <c>Id</c> is the
/// explicit primary key. The link to the parent reference still goes through the
/// <see cref="ParentReferenceCode"/> TEXT as it did in the legacy system (the IBYS XML works
/// with codes); the <see cref="IbysRootReferenceValueId"/> FK was added on top of it to make
/// joins easier. There are NO navigation properties.
/// </para>
/// <para>Host reference table — does NOT implement <c>IMultiTenant</c>.</para>
/// </summary>
public class IbysChildReferenceValue : AuditedEntity, IActivatable
{
    /// <summary>Reference code defined by IBYS. (Legacy: <c>Kod</c>)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name of the reference. (Legacy: <c>ReferansAdi</c>)</summary>
    public string ReferenceName { get; set; } = string.Empty;

    /// <summary>Code of the parent reference. (Legacy: <c>UstReferansKodu</c>)</summary>
    public string ParentReferenceCode { get; set; } = string.Empty;

    /// <summary>
    /// NORMALISATION (new column): FK of the parent reference record.
    /// It is populated during seeding by matching on <see cref="ParentReferenceCode"/>.
    /// </summary>
    public int? IbysRootReferenceValueId { get; set; }

    /// <summary>Whether the reference is still in use. (Legacy: <c>AktifMi</c>)</summary>
    public bool IsActive { get; set; } = true;
}
