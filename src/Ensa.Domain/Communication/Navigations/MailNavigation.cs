using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Documents;

namespace Ensa.Domain.Communication.Navigations;

/// <summary>
/// Combined view of a <see cref="Mail"/> and its attachments, including document names.
/// <para>
/// RULE: it is <c>[NotMapped]</c>, never a <c>DbSet</c>, and never added to <c>ModelBuilder</c>.
/// <c>IMailRepository</c> populates it through an <c>IQueryable</c> join and projection.
/// </para>
/// </summary>
[NotMapped]
public class MailNavigation : NavigationEntity
{
    /// <summary>The mapped root entity.</summary>
    public Mail Mail { get; set; } = null!;

    /// <summary>Attachments, together with document name and extension.</summary>
    public List<MailAttachmentNavigation> Attachments { get; set; } = [];
}

/// <summary>Combined view of a <see cref="MailAttachment"/> and its document.</summary>
[NotMapped]
public class MailAttachmentNavigation : NavigationEntity
{
    public MailAttachment MailAttachment { get; set; } = null!;

    public Document? Document { get; set; }
}
