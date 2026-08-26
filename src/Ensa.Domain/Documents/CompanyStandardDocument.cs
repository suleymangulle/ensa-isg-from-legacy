using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Documents;

/// <summary>
/// The concrete document a company submitted, and had approved, for a given standard document type.
/// <para>Legacy equivalent: <c>StandardDocumentsCompany_T</c>.</para>
/// <para>
/// The legacy pair of <c>Status</c> (int?) and <c>ApprovalStatus</c> (int?) columns was merged
/// into a single <see cref="ApprovalStatus"/> enum field.
/// </para>
/// </summary>
public class CompanyStandardDocument : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The standard document type. FK — no navigation property.</summary>
    public int StandardDocumentId { get; set; }

    /// <summary>The related company. FK — no navigation property.</summary>
    public int CompanyId { get; set; }

    /// <summary>FK to the central <c>Document</c> table.</summary>
    public int? DocumentId { get; set; }

    /// <summary>
    /// Approval workflow status. (Legacy: the merged form of the <c>Durum</c> + <c>ApprovalStatus</c>
    /// int? pair.)
    /// </summary>
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;

    /// <summary>Issue or validity date of the document.</summary>
    public DateTime? DocumentDate { get; set; }
}
