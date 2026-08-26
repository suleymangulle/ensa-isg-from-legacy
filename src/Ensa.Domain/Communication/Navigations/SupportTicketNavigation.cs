using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;

namespace Ensa.Domain.Communication.Navigations;

/// <summary>
/// Combined view of a <see cref="SupportTicket"/> and its messages
/// (<see cref="SupportTicketMessage"/>).
/// <para>
/// RULE: it is <c>[NotMapped]</c>, never a <c>DbSet</c>, and never added to <c>ModelBuilder</c>.
/// <c>ISupportTicketRepository</c> populates it through an <c>IQueryable</c> join and projection.
/// </para>
/// </summary>
[NotMapped]
public class SupportTicketNavigation : NavigationEntity
{
    /// <summary>The mapped root entity.</summary>
    public SupportTicket SupportTicket { get; set; } = null!;

    /// <summary>The ticket's message history, in chronological order.</summary>
    public List<SupportTicketMessage> Messages { get; set; } = [];
}
