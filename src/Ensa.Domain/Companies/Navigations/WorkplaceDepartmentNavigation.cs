using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;

namespace Ensa.Domain.Companies.Navigations;

/// <summary>
/// Combined read model for a <see cref="WorkplaceDepartment"/> record — the department, the
/// company it belongs to, its documents and the employees working in it.
/// <para>RULE: it is <see cref="NotMappedAttribute"/> and never becomes a <c>DbSet</c>.</para>
/// </summary>
[NotMapped]
public class WorkplaceDepartmentNavigation : NavigationEntity
{
    /// <summary>The root (mapped) entity.</summary>
    public WorkplaceDepartment WorkplaceDepartment { get; set; } = null!;

    public Company Company { get; set; } = null!;

    public List<DepartmentDocument> Documents { get; set; } = [];

    /// <summary>The employees working in this department.</summary>
    public List<CompanyEmployee> Employees { get; set; } = [];
}
