using Ensa.Domain.Common;

namespace Ensa.Domain.Companies;

/// <summary>
/// An activity/document definition included in the company's service scope.
/// <para>Legacy equivalent: <c>CompanyActivity_T</c>.</para>
/// <para>
/// NORMALISATION: the legacy <c>ActivityCode</c> was free text; it was normalised into an
/// <see cref="ActivityId"/> FK to the <c>Activity</c> table. The legacy code is still kept in
/// <see cref="ActivityCode"/> to avoid data loss.
/// </para>
/// </summary>
public class CompanyActivity : CreationAuditedTenantEntity, ICompanyScoped
{
    public int CompanyId { get; set; }

    /// <summary>FK to the activity definition table.</summary>
    public int ActivityId { get; set; }

    /// <summary>Legacy activity code (a migration trace, kept for backward matching).</summary>
    public string? ActivityCode { get; set; }
}
