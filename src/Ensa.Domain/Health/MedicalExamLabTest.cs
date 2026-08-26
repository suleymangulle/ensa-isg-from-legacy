using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Health;

/// <summary>
/// Laboratory/diagnostic test line of an examination form.
/// <para>
/// NORMALISATION: this merges two separate column groups of the legacy
/// <c>PeriodicExaminationForm_T</c> into a single table:
/// <list type="bullet">
/// <item>Result texts: <c>Blood</c>, <c>Urine</c>, <c>RadiologicalAnaliz</c>,
/// <c>Audiometry</c>, <c>SFT</c>, <c>PsychologicalTestler</c>, <c>LabOther</c>
/// → <see cref="Result"/></item>
/// <item>"Was it performed" flags: <c>BloodTetkikiCompletedMi</c>, <c>UrineTetkikiCompletedMi</c>,
/// <c>RontgenCompletedMi</c>, <c>HearingTestCompletedMi</c>, <c>RespiratoryFontTestCompletedMi</c>
/// → <see cref="IsCompleted"/></item>
/// </list>
/// No SEPARATE table was created for radiological tests;
/// <see cref="LabTestType.RadiologicalImaging"/> is just a row of this table.
/// </para>
/// <para>
/// Unique constraint: (<see cref="MedicalExaminationFormId"/>, <see cref="LabTestType"/>).
/// </para>
/// </summary>
public class MedicalExamLabTest : FullAuditedTenantEntity
{
    public int MedicalExaminationFormId { get; set; }

    /// <summary>Test type. (Legacy: the column name itself)</summary>
    public LabTestType LabTestType { get; set; }

    /// <summary>
    /// Whether the test was performed. This is a mandatory field on the IBYS submission.
    /// (Legacy: the <c>*YapildiMi</c> string columns)
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>Test result/report (free text). ENCRYPTED COLUMN.</summary>
    public string? Result { get; set; }

    /// <summary>Date the test was performed.</summary>
    public DateTime? Date { get; set; }
}
