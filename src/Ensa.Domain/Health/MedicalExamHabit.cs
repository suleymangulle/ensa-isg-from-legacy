using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Health;

/// <summary>
/// Habit line (tobacco / alcohol / substance) of an examination form.
/// <para>
/// NORMALISATION: this replaces two column groups of the legacy
/// <c>PeriodicExaminationForm_T</c>:
/// <list type="bullet">
/// <item>Tobacco: <c>SmokingIciyorMusunuz</c>, <c>SmokingCount</c>, <c>SmokingDuration</c>,
/// <c>SmokingDurationTime</c>, <c>SmokingDurationBefore</c>, <c>SmokingDurationBeforeTime</c></item>
/// <item>Alcohol: <c>AlcoholIciyorMusunuz</c>, <c>AlcoholSiklikla</c>, <c>AlcoholDuration</c>,
/// <c>ALkolYearIcmis</c>, <c>AlcoholYearBefore</c></item>
/// </list>
/// The legacy schema kept "duration" and "how many years ago they quit" in both text and numeric
/// columns; here they are reduced to a single pair of numeric fields.
/// </para>
/// <para>
/// Unique constraint: (<see cref="MedicalExaminationFormId"/>, <see cref="HabitType"/>).
/// </para>
/// </summary>
public class MedicalExamHabit : FullAuditedTenantEntity
{
    public int MedicalExaminationFormId { get; set; }

    /// <summary>Habit type. (Legacy: the prefix of the column group)</summary>
    public HabitType HabitType { get; set; }

    /// <summary>
    /// Consumption status.
    /// (Legacy: the <c>SigaraIciyorMusunuz</c> / <c>AlcoholIciyorMusunuz</c> string values)
    /// </summary>
    public HabitStatus Status { get; set; } = HabitStatus.Unspecified;

    /// <summary>
    /// Daily quantity — cigarettes per day for tobacco, standard drinks per day for alcohol.
    /// (Legacy: <c>SigaraAdet</c>)
    /// </summary>
    public int? DailyQuantity { get; set; }

    /// <summary>
    /// Total duration of use, in years. (Legacy: <c>SigaraSure</c> plus the
    /// <c>SmokingDurationTime</c> unit text, <c>AlcoholDuration</c> / <c>ALkolYearIcmis</c>)
    /// </summary>
    public int? DurationYear { get; set; }

    /// <summary>
    /// How many years ago the habit was given up, when <see cref="HabitStatus.Quit"/>.
    /// (Legacy: <c>SigaraSureOnce</c> + <c>SmokingDurationBeforeTime</c>, <c>AlcoholYearBefore</c>)
    /// </summary>
    public int? CessationYearBefore { get; set; }

    /// <summary>
    /// Free-text note — a statement of frequency and the like. ENCRYPTED COLUMN.
    /// (Legacy: <c>AlkolSiklikla</c>)
    /// </summary>
    public string? Description { get; set; }
}
