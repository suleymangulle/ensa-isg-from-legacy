using System.ComponentModel.DataAnnotations;
using Ensa.Application.Contracts.Common;
using Ensa.Domain.Shared;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Application.Contracts.Health.Dtos;

// ---------------------------------------------------------------------------
// Normalised child collections of a medical examination form.
//
// PRIVACY: every type in this file carries clinical content. These DTOs are
// returned only as part of a single, explicitly requested form (detail view or
// a child-collection save call) and never as part of a list payload.
// ---------------------------------------------------------------------------

/// <summary>A complaint / history line of the examination form.</summary>
public class MedicalExamComplaintDto : EntityDto
{
    public int MedicalExaminationFormId { get; set; }

    public MedicalComplaintType ComplaintType { get; set; }

    public TriStateAnswer Answer { get; set; }

    public string? Description { get; set; }
}

/// <summary>One complaint line of the replacement set sent to <c>SaveComplaintsAsync</c>.</summary>
public class SaveMedicalExamComplaintDto
{
    [EnumDataType(typeof(MedicalComplaintType), ErrorMessage = "An unknown complaint type was supplied.")]
    public MedicalComplaintType ComplaintType { get; set; }

    [EnumDataType(typeof(TriStateAnswer))]
    public TriStateAnswer Answer { get; set; } = TriStateAnswer.Unspecified;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Description { get; set; }
}

/// <summary>A physical examination finding, per body system.</summary>
public class MedicalExamPhysicalFindingDto : EntityDto
{
    public int MedicalExaminationFormId { get; set; }

    public PhysicalExamSystem System { get; set; }

    public ExamFinding Finding { get; set; }

    public string? Description { get; set; }
}

/// <summary>One finding of the replacement set sent to <c>SavePhysicalFindingsAsync</c>.</summary>
public class SaveMedicalExamPhysicalFindingDto
{
    [EnumDataType(typeof(PhysicalExamSystem), ErrorMessage = "An unknown body system was supplied.")]
    public PhysicalExamSystem System { get; set; }

    [EnumDataType(typeof(ExamFinding))]
    public ExamFinding Finding { get; set; } = ExamFinding.Unspecified;

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Description { get; set; }
}

/// <summary>A laboratory / diagnostic test line.</summary>
public class MedicalExamLabTestDto : EntityDto
{
    public int MedicalExaminationFormId { get; set; }

    public LabTestType LabTestType { get; set; }

    /// <summary>Whether the test was actually performed — a mandatory IBYS field.</summary>
    public bool IsCompleted { get; set; }

    public string? Result { get; set; }

    public DateTime? Date { get; set; }
}

/// <summary>One test of the replacement set sent to <c>SaveLabTestsAsync</c>.</summary>
public class SaveMedicalExamLabTestDto
{
    [EnumDataType(typeof(LabTestType), ErrorMessage = "An unknown laboratory test type was supplied.")]
    public LabTestType LabTestType { get; set; }

    public bool IsCompleted { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Note)]
    public string? Result { get; set; }

    public DateTime? Date { get; set; }
}

/// <summary>A habit (smoking / alcohol / substance) line.</summary>
public class MedicalExamHabitDto : EntityDto
{
    public int MedicalExaminationFormId { get; set; }

    public HabitType HabitType { get; set; }

    public HabitStatus Status { get; set; }

    public int? DailyQuantity { get; set; }

    public int? DurationYear { get; set; }

    public int? CessationYearBefore { get; set; }

    public string? Description { get; set; }
}

/// <summary>One habit of the replacement set sent to <c>SaveHabitsAsync</c>.</summary>
public class SaveMedicalExamHabitDto
{
    [EnumDataType(typeof(HabitType), ErrorMessage = "An unknown habit type was supplied.")]
    public HabitType HabitType { get; set; }

    [EnumDataType(typeof(HabitStatus))]
    public HabitStatus Status { get; set; } = HabitStatus.Unspecified;

    [Range(0, 500)]
    public int? DailyQuantity { get; set; }

    [Range(0, 100)]
    public int? DurationYear { get; set; }

    [Range(0, 100)]
    public int? CessationYearBefore { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Description { get; set; }
}

/// <summary>A "is the employee fit for this working condition?" assessment line.</summary>
public class MedicalExamWorkConditionDto : EntityDto
{
    public int MedicalExaminationFormId { get; set; }

    public WorkConditionType ConditionType { get; set; }

    public TriStateAnswer Suitable { get; set; }
}

/// <summary>One condition of the replacement set sent to <c>SaveWorkConditionsAsync</c>.</summary>
public class SaveMedicalExamWorkConditionDto
{
    [EnumDataType(typeof(WorkConditionType), ErrorMessage = "An unknown working condition was supplied.")]
    public WorkConditionType ConditionType { get; set; }

    [EnumDataType(typeof(TriStateAnswer))]
    public TriStateAnswer Suitable { get; set; } = TriStateAnswer.Unspecified;
}

/// <summary>An immunisation declared during the examination.</summary>
public class MedicalExamImmunizationDto : EntityDto
{
    public int MedicalExaminationFormId { get; set; }

    public ImmunizationType ImmunizationType { get; set; }

    public DateTime? Date { get; set; }

    public string? Description { get; set; }
}

/// <summary>One immunisation of the replacement set sent to <c>SaveImmunizationsAsync</c>.</summary>
public class SaveMedicalExamImmunizationDto
{
    [EnumDataType(typeof(ImmunizationType), ErrorMessage = "An unknown immunisation type was supplied.")]
    public ImmunizationType ImmunizationType { get; set; }

    public DateTime? Date { get; set; }

    [MaxLength(EnsaDomainSharedConsts.MaxLengths.Text)]
    public string? Description { get; set; }
}
