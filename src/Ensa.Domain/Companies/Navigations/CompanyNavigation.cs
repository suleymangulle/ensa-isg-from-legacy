using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Lookups;

namespace Ensa.Domain.Companies.Navigations;

/// <summary>
/// Combined (joined) read model for a <see cref="Company"/> record.
/// <para>
/// RULE: it is <see cref="NotMappedAttribute"/>, never becomes a <c>DbSet</c> and is never
/// registered with <c>ModelBuilder</c>. It is populated through an <c>IQueryable</c> join plus
/// projection inside <c>ICompanyRepository.GetWithNavigationAsync</c>.
/// </para>
/// <para>
/// The <c>[NotMapped]</c> members <c>Apply</c>, <c>RelatedTckNo</c> and
/// <c>AssignedSpecialists</c> of the legacy <c>Company_T</c> were removed from the entity and
/// moved here.
/// </para>
/// </summary>
[NotMapped]
public class CompanyNavigation : NavigationEntity
{
    /// <summary>The root (mapped) entity.</summary>
    public Company Company { get; set; } = null!;

    // ---------------- Address lookups (host reference tables) ----------------

    public City? City { get; set; }

    public District? District { get; set; }

    public Neighborhood? Neighborhood { get; set; }

    // ---------------- Headquarter / branch ----------------

    /// <summary>When this record is a branch, the headquarter company it belongs to.</summary>
    public Company? HeadquarterCompany { get; set; }

    /// <summary>When this record is a headquarter, the branches attached to it.</summary>
    public List<Company> Branches { get; set; } = [];

    // ---------------- Child collections ----------------

    public List<CompanyEmployee> Employees { get; set; } = [];

    /// <summary>Specialist/physician assignments for the company.</summary>
    public List<AssignedSpecialist> AssignedSpecialists { get; set; } = [];

    public List<WorkplaceDepartment> Departments { get; set; } = [];

    // ---------------- Summary ----------------

    /// <summary>Outstanding obligation summary computed by the background job, when present.</summary>
    public CompanyComplianceSummary? Warning { get; set; }

    // ---------------- Members moved from legacy [NotMapped] ----------------

    /// <summary>
    /// National IDs of the specialists/physicians assigned to the company (used in IBYS
    /// submissions).
    /// (Legacy: <c>Firma_T.IlgiliTckNo</c> <c>[NotMapped]</c>)
    /// </summary>
    public List<string> RelatedTcMembershipNumbers { get; set; } = [];

    /// <summary>
    /// Whether the row is selected on bulk-action screens.
    /// (Legacy: <c>Firma_T.Uygula</c> <c>[NotMapped]</c>)
    /// </summary>
    public bool Apply { get; set; }
}
