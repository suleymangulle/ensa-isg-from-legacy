using Ensa.Domain.Common;

namespace Ensa.Domain.Companies;

/// <summary>
/// A physical/organisational department of a workplace (e.g. "welding shop", "administration
/// building"). Risk assessments, equipment and employee records hang off it.
/// <para>Legacy equivalent: <c>WorkplaceDepartment_T</c> (PK <c>DepartmentId</c>).</para>
/// </summary>
public class WorkplaceDepartment : FullAuditedTenantEntity, ICompanyScoped
{
    public int CompanyId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the record may be deleted by a user. It is <c>false</c> for the default
    /// departments the system creates automatically. (Legacy: <c>Deletable</c>)
    /// </summary>
    public bool IsDeletable { get; set; } = true;
}
