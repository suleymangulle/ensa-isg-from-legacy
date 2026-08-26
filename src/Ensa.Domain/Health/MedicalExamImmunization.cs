using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Health;

/// <summary>
/// Immunization (vaccination) line declared during an examination.
/// <para>
/// NORMALISATION: this replaces the legacy
/// <c>PeriodicExaminationForm_T.BagisiklamaTetanus</c> / <c>BagisiklamaHepatitis</c> /
/// <c>BagisiklamaOther</c> columns.
/// </para>
/// <para>
/// <b>NOTE — do not confuse this with <c>Companies.EmployeeImmunization</c>.</b>
/// <list type="bullet">
/// <item><c>EmployeeImmunization</c>: the employee's PERMANENT vaccination record; it hangs off
/// the employee, lives independently of any examination and is updated over time.</item>
/// <item><see cref="MedicalExamImmunization"/> (this class): what was DECLARED AT THAT
/// EXAMINATION; it hangs off the form, is a historical snapshot of it and never changes
/// afterwards.</item>
/// </list>
/// These are separate tables; copying a declaration from an examination into the employee's
/// vaccination record is an explicit operation in the application layer — there is NO automatic
/// synchronisation.
/// </para>
/// <para>
/// Unique constraint: (<see cref="MedicalExaminationFormId"/>, <see cref="ImmunizationType"/>).
/// </para>
/// </summary>
public class MedicalExamImmunization : FullAuditedTenantEntity
{
    public int MedicalExaminationFormId { get; set; }

    /// <summary>Vaccine type. (Legacy: the suffix of the column name)</summary>
    public ImmunizationType ImmunizationType { get; set; }

    /// <summary>Date the vaccine was administered (as declared). ENCRYPTED COLUMN.</summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Dose/free-text note. ENCRYPTED COLUMN.
    /// For <see cref="ImmunizationType.Other"/> it carries the vaccine name.
    /// (Legacy: <c>BagisiklamaDiger</c>)
    /// </summary>
    public string? Description { get; set; }
}
