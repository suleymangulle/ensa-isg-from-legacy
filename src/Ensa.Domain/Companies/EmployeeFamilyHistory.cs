using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Companies;

/// <summary>
/// An employee's family history (diseases known in the family). 1-N with
/// <see cref="CompanyEmployee"/>.
/// <para>
/// NORMALISATION: this replaces the <c>FamilyHistoryMother</c>, <c>FamilyHistoryFather</c>,
/// <c>FamilyHistorySibling</c>, <c>FamilyHistoryChild</c> and <c>FamilyHistoryOther</c> columns
/// of the legacy <c>CompanyEmployee_T</c>.
/// </para>
/// </summary>
public class EmployeeFamilyHistory : FullAuditedTenantEntity
{
    public int CompanyEmployeeId { get; set; }

    /// <summary>The relative the disease was reported for.</summary>
    public FamilyRelation Relation { get; set; }

    /// <summary>The reported disease(s) — the free text from the legacy column.</summary>
    public string? Description { get; set; }
}
