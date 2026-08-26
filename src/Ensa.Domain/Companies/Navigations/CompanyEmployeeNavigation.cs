using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;

namespace Ensa.Domain.Companies.Navigations;

/// <summary>
/// Combined read model for a <see cref="CompanyEmployee"/> record — identity, employment and the
/// normalised health child records together.
/// <para>RULE: it is <see cref="NotMappedAttribute"/> and never becomes a <c>DbSet</c>.</para>
/// </summary>
[NotMapped]
public class CompanyEmployeeNavigation : NavigationEntity
{
    /// <summary>The root (mapped) entity.</summary>
    public CompanyEmployee CompanyEmployee { get; set; } = null!;

    /// <summary>The workplace the employee works at.</summary>
    public Company Company { get; set; } = null!;

    /// <summary>The department the employee works in, when assigned.</summary>
    public WorkplaceDepartment? AssignedDepartment { get; set; }

    // ---------------- Health (normalised child records) ----------------

    /// <summary>The 1-1 health information record (blood type, allergies, chronic illness).</summary>
    public EmployeeHealthInfo? HealthInfo { get; set; }

    public List<EmployeeImmunization> Immunizations { get; set; } = [];

    public List<EmployeeFamilyHistory> FamilyHistory { get; set; } = [];

    public List<EmployeeWorkHistory> WorkHistory { get; set; } = [];

    // ---------------- Duties / training ----------------

    public List<CompanyEmployeeDuty> Duties { get; set; } = [];

    /// <summary>The employee's latest attendance date per training (a query projection).</summary>
    public List<EmployeeLatestTrainingInfo> LatestTrainings { get; set; } = [];
}
