using Ensa.Domain.Common;

namespace Ensa.Domain.Documents;

/// <summary>
/// Document category reference, e.g. "Risk Assessment Report" or "Health Report".
/// <para>Legacy equivalent: <c>DocumentCategory_T</c>.</para>
/// </summary>
public class DocumentCategory : AuditedTenantEntity
{
    /// <summary>Unique category code.</summary>
    public string CategoryCode { get; set; } = string.Empty;

    /// <summary>Category name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>The related reporting article group, if any.</summary>
    public int? ReportingArticleGroup { get; set; }
}
