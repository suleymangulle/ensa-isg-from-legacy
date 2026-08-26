using Ensa.Domain.Common;

namespace Ensa.Domain.Communication;

/// <summary>
/// NEW ENTITY. A document attached to a <see cref="Mail"/>; the normalized form of the legacy
/// <c>Mail_T.BagliDocuments</c> CSV string column.
/// </summary>
public class MailAttachment : CreationAuditedTenantEntity
{
    /// <summary>FK — no navigation property.</summary>
    public int MailId { get; set; }

    /// <summary>FK to the central <c>Document</c> table.</summary>
    public int DocumentId { get; set; }

    public int OrderNo { get; set; }
}
