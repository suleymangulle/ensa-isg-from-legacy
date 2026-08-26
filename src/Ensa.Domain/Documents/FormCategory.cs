using Ensa.Domain.Common;

namespace Ensa.Domain.Documents;

/// <summary>
/// Form category reference, e.g. "Risk Assessment Forms".
/// <para>Legacy equivalent: <c>FormCategory_T</c>.</para>
/// </summary>
public class FormCategory : AuditedTenantEntity
{
    /// <summary>Category name.</summary>
    public string CategoryName { get; set; } = string.Empty;
}
