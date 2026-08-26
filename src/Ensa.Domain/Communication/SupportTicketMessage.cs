using Ensa.Domain.Common;

namespace Ensa.Domain.Communication;

/// <summary>
/// A single message on a <see cref="SupportTicket"/>.
/// <para>Legacy equivalent: <c>UserRequestMessage_T</c>.</para>
/// </summary>
public class SupportTicketMessage : CreationAuditedTenantEntity
{
    /// <summary>FK — no navigation property.</summary>
    public int SupportTicketId { get; set; }

    /// <summary>(Legacy: <c>Message</c>)</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>(Legacy: <c>GonderenId</c>) FK — no navigation property.</summary>
    public int SenderUserId { get; set; }

    /// <summary>(Legacy: <c>AlanId</c>) FK — no navigation property.</summary>
    public int FieldUserId { get; set; }

    /// <summary>(Legacy: <c>IsRead</c>)</summary>
    public bool IsRead { get; set; }

    // NOTE: the legacy MessageDate is covered by the base class CreationTime, so no separate
    // field was added.
}
