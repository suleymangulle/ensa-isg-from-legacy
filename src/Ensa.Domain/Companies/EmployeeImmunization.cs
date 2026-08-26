using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Companies;

/// <summary>
/// An employee's vaccination/immunization record. 1-N with <see cref="CompanyEmployee"/>.
/// <para>
/// NORMALISATION: this replaces the <c>Tetanus</c>, <c>Hepatitis</c>, <c>Influenza</c>,
/// <c>Other01</c> and <c>Other02</c> columns of the legacy <c>CompanyEmployee_T</c>.
/// </para>
/// </summary>
public class EmployeeImmunization : FullAuditedTenantEntity
{
    public int CompanyEmployeeId { get; set; }

    /// <summary>Vaccine type. When <c>Other</c> is selected the detail goes into <see cref="Description"/>.</summary>
    public ImmunizationType ImmunizationType { get; set; }

    /// <summary>Date the vaccine was administered.</summary>
    public DateTime? Date { get; set; }

    /// <summary>Dose information, the administering institution, or the legacy free text.</summary>
    public string? Description { get; set; }
}
