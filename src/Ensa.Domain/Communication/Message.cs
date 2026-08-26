using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Communication;

/// <summary>
/// An in-application message between users or employees.
/// <para>Legacy equivalent: <c>Mesajlasma_T</c>.</para>
/// <para>
/// CAUTION: this is NOT the same thing as <c>Ensa.Domain.Lookups.Message</c> (the dictionary of
/// in-application notification texts, legacy <c>Message_T</c>). The clash is in the name only;
/// the namespaces differ.
/// </para>
/// </summary>
public class Message : CreationAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The kind of parties exchanging the message. (Legacy: <c>MesajTip</c> enum)</summary>
    public MessageType MessageType { get; set; }

    /// <summary>(Legacy: <c>Mesaj</c>)</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Recipient user or employee. FK — no navigation property.</summary>
    public int RecipientId { get; set; }

    /// <summary>Sending user or employee. FK — no navigation property.</summary>
    public int SenderId { get; set; }

    /// <summary>The company the message relates to, if any. FK — no navigation property.</summary>
    public int? CompanyId { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadDate { get; set; }

    // NOTE: the legacy GonderimTarihi is covered by the base class CreationTime, so no separate
    // field was added. The legacy static factory methods (KullaniciMesaj/PersonelAliciMesaj/
    // PersonelGondericiMesaj) were NOT carried over — if the business rule is needed it belongs in
    // a domain service.
}
