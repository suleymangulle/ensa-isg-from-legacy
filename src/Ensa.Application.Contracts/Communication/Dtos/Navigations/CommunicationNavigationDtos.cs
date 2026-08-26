using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Communication.Dtos.Navigations;

/// <summary>
/// A mail together with its attachments, resolved to file names.
/// <para>
/// Class-typed properties are forbidden on ordinary DTOs, so combined reads live in a
/// <see cref="NavigationDto"/> derivative (see docs/ARCHITECTURE.md section 4).
/// </para>
/// </summary>
public class MailNavigationDto : NavigationDto
{
    public MailDto Mail { get; set; } = null!;

    /// <summary>Attachments in <c>OrderNo</c> order, each carrying its document name.</summary>
    public List<MailAttachmentNavigationDto> Attachments { get; set; } = [];
}

/// <summary>An attachment row together with the name of the file behind it.</summary>
public class MailAttachmentNavigationDto : NavigationDto
{
    public MailAttachmentDto Attachment { get; set; } = null!;

    /// <summary>The stored document: id, display name and extension as the code.</summary>
    public LookupDto? Document { get; set; }
}

/// <summary>
/// A support ticket with the people involved and the full message thread — everything the
/// ticket detail screen needs in one call.
/// </summary>
public class SupportTicketNavigationDto : NavigationDto
{
    public SupportTicketDto SupportTicket { get; set; } = null!;

    /// <summary>The user who opened the ticket.</summary>
    public LookupDto? OpenedByUser { get; set; }

    /// <summary>The support user handling the ticket, once one has replied.</summary>
    public LookupDto? ResponderUser { get; set; }

    /// <summary>The message thread, oldest first.</summary>
    public List<SupportTicketMessageDto> Messages { get; set; } = [];
}
