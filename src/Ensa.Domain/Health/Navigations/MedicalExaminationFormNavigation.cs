using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;
using Ensa.Domain.Companies;

namespace Ensa.Domain.Health.Navigations;

/// <summary>
/// The examination form in a shape that can be rendered on screen or in a report:
/// the form header plus the employee, the workplace and every normalised child list.
/// <para>
/// The legacy schema kept all of this copied into one enormous table; it is now joined through
/// <c>MedicalExaminationForm.CompanyId</c> / <c>CompanyEmployeeId</c> and projected into this
/// class.
/// </para>
/// <para>
/// <c>[NotMapped]</c> — never exposed as a <c>DbSet</c> and never registered with
/// <c>ModelBuilder</c>; it is populated through an <c>IQueryable</c> join plus projection inside
/// <c>IMedicalExaminationFormRepository.GetWithNavigationAsync</c>.
/// </para>
/// </summary>
[NotMapped]
public class MedicalExaminationFormNavigation : NavigationEntity<MedicalExaminationForm>
{
    /// <summary>Shortcut to the root record (the same instance as <see cref="NavigationEntity{TEntity}.Entity"/>).</summary>
    public MedicalExaminationForm Form
    {
        get => Entity;
        set => Entity = value;
    }

    /// <summary>The employee who was examined — the source of the personal details the legacy form copied in.</summary>
    public CompanyEmployee? Employee { get; set; }

    /// <summary>The workplace where the examination took place — the source of the workplace details the legacy form copied in.</summary>
    public Company? Company { get; set; }

    /// <summary>Full name of the workplace physician who performed the examination (lookup — the <c>User</c> table lives in the Membership module).</summary>
    public string? PhysicianFullName { get; set; }

    // ---------------- Normalised child lists ----------------

    public List<MedicalExamComplaint> Complaints { get; set; } = [];

    public List<MedicalExamPhysicalFinding> FizikFindings { get; set; } = [];

    public List<MedicalExamLabTest> LabTests { get; set; } = [];

    public List<MedicalExamHabit> Habits { get; set; } = [];

    public List<MedicalExamWorkCondition> WorkConditions { get; set; } = [];

    public List<MedicalExamImmunization> Immunizations { get; set; } = [];

    // ---------------- Derived indicators ----------------

    /// <summary>
    /// Date of this employee's previous examination, when there is one — shown on the periodic
    /// follow-up screen.
    /// </summary>
    public DateTime? PreviousExaminationDate { get; set; }

    /// <summary>Number of the IBYS query this form belongs to (lookup — <c>IbysQuery.QueryNo</c>).</summary>
    public string? IbysQueryNo { get; set; }
}
