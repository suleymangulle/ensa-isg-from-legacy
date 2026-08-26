using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Health.Dtos.Navigations;

/// <summary>
/// Everything the medical examination form detail screen needs in a single call:
/// the form itself, the examined employee, the workplace and all six normalised
/// child collections.
/// <para>
/// DTOs may not declare class-typed properties, so this composition lives in a
/// <see cref="NavigationDto"/> subtype (see docs/ARCHITECTURE.md §4).
/// </para>
/// <para>
/// <b>PRIVACY.</b> This is the only shape in the module that exposes the complete
/// clinical picture of an employee. It is returned for one explicitly requested
/// record at a time and is guarded by <c>Ensa.MedicalExamination</c>. Employee and
/// workplace are reduced to lookups so that unrelated personal data (national id,
/// home address, next of kin) never travels with a health record.
/// </para>
/// </summary>
public class MedicalExaminationFormNavigationDto : NavigationDto
{
    public MedicalExaminationFormDto Form { get; set; } = null!;

    /// <summary>Examined employee, reduced to a lookup.</summary>
    public LookupDto? Employee { get; set; }

    /// <summary>Workplace where the examination took place, reduced to a lookup.</summary>
    public LookupDto? Company { get; set; }

    /// <summary>Examining occupational physician's display name.</summary>
    public string? PhysicianFullName { get; set; }

    // ---------------- Normalised child collections ----------------

    public List<MedicalExamComplaintDto> Complaints { get; set; } = [];

    public List<MedicalExamPhysicalFindingDto> PhysicalFindings { get; set; } = [];

    public List<MedicalExamLabTestDto> LabTests { get; set; } = [];

    public List<MedicalExamHabitDto> Habits { get; set; } = [];

    public List<MedicalExamWorkConditionDto> WorkConditions { get; set; } = [];

    public List<MedicalExamImmunizationDto> Immunizations { get; set; } = [];

    // ---------------- Derived indicators ----------------

    /// <summary>Date of this employee's previous examination, when one exists.</summary>
    public DateTime? PreviousExaminationDate { get; set; }

    /// <summary>Query number of the IBYS submission this form belongs to.</summary>
    public string? IbysQueryNo { get; set; }
}
