using Ensa.Domain.Common;

namespace Ensa.Domain.Ibys;

/// <summary>
/// Licence for the e-signature component used to sign IBYS submissions.
/// <para>Legacy equivalent: <c>ArksignerLicense_T</c> (renamed after its function rather than
/// the product).</para>
/// <para>
/// Host table — does NOT implement <c>IMultiTenant</c>; the licence is shared by every
/// organization. The legacy <c>EklenmeDate</c> column corresponds to <c>CreationTime</c> on the
/// base class, so it is not declared again here.
/// </para>
/// </summary>
public class ESignatureLicense : AuditedEntity, IActivatable
{
    /// <summary>
    /// Licence key. ENCRYPTED COLUMN — this is a secret and must never be mapped into a DTO.
    /// (Legacy: <c>Lisans</c>)
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>Date the licence expires. (Legacy: <c>GecerlilikTarihi</c>)</summary>
    public DateTime ValidityDate { get; set; }

    /// <summary>Whether the licence is in use. (Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
