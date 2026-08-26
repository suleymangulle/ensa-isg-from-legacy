using Ensa.Domain.Common;

namespace Ensa.Domain.Companies;

/// <summary>
/// An employee's previous employment history. 1-N with <see cref="CompanyEmployee"/>.
/// <para>
/// NORMALISATION: this replaces the repeated <c>PreviousIsIskolu1/2/3</c>,
/// <c>PreviousIsPerformedIs1/2/3</c>, <c>PreviousIsEntry1/2/3</c> and
/// <c>PreviousIsExit1/2/3</c> column group of the legacy periodic/pre-employment examination
/// form.
/// </para>
/// <para>
/// The health module (<c>MedicalExaminationForm</c>) references these records by FK; they are no
/// longer copied onto the examination form.
/// </para>
/// </summary>
public class EmployeeWorkHistory : FullAuditedTenantEntity
{
    public int CompanyEmployeeId { get; set; }

    /// <summary>Industry/sector worked in.</summary>
    public string? WorkSector { get; set; }

    /// <summary>Work/duty performed at that workplace.</summary>
    public string? PerformedJob { get; set; }

    public DateTime? EntryDate { get; set; }

    public DateTime? ExitDate { get; set; }

    /// <summary>Position on the form (the index of the repeated legacy 1/2/3 group). 1 is the most recent job.</summary>
    public int OrderNo { get; set; }
}
