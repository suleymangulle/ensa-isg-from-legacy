using Ensa.Domain.Common;

namespace Ensa.Domain.Companies;

/// <summary>
/// Free-form definition/tag specific to a company (used as a placeholder in reports and
/// templates).
/// <para>Legacy equivalent: <c>CompanyTag_T</c>.</para>
/// </summary>
public class CompanyTag : CreationAuditedTenantEntity, ICompanyScoped
{
    public int CompanyId { get; set; }

    /// <summary>Code of the definition (the key referenced from templates).</summary>
    public string TagCode { get; set; } = string.Empty;

    /// <summary>Value/text of the definition.</summary>
    public string? Tag { get; set; }
}
