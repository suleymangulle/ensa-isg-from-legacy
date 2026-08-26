using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Communication;

/// <summary>
/// The header of a support ticket raised by a user. The messages live in
/// <see cref="SupportTicketMessage"/>.
/// <para>Legacy equivalent: <c>UserRequest_T</c>.</para>
/// </summary>
public class SupportTicket : CreationAuditedTenantEntity
{
    /// <summary>(Legacy: <c>Subject</c>)</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>The user who opened the ticket. (Legacy: <c>StartedId</c>) FK — no navigation property.</summary>
    public int OpenedByUserId { get; set; }

    /// <summary>The support user who answered the ticket. (Legacy: <c>AnsweredId</c>) FK — no navigation property.</summary>
    public int? ResponderUserId { get; set; }

    /// <summary>The user who closed the ticket. (Legacy: <c>EndedId</c>) FK — no navigation property.</summary>
    public int? ClosedByUserId { get; set; }

    /// <summary>(Legacy: <c>IsClosed</c> bool)</summary>
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;

    /// <summary>(Legacy: <c>EndingTime</c>)</summary>
    public DateTime? ClosingDate { get; set; }

    // NOTE: the legacy BeginingTime is covered by the base class CreationTime, so no separate
    // field was added.
}
