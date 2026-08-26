using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Finance;

/// <summary>
/// NEW ENTITY. The amount a <see cref="Penalty"/> article carries for a given workplace hazard
/// class and employee count range; the normalized form of the nine-column fixed matrix in legacy
/// (see the <see cref="Penalty"/> XML doc).
/// <para>
/// <see cref="ValidityYear"/> also makes per-year tracking possible: legacy could hold only a
/// single "current" amount, so previous years' amounts were lost.
/// </para>
/// <para>Unique on (<see cref="PenaltyId"/>, <see cref="HazardClass"/>, <see cref="EmployeeCountRange"/>, <see cref="ValidityYear"/>).</para>
/// </summary>
public class PenaltyAmount : AuditedEntity
{
    /// <summary>FK — no navigation property.</summary>
    public int PenaltyId { get; set; }

    public HazardClass HazardClass { get; set; }

    public EmployeeCountRange EmployeeCountRange { get; set; }

    public decimal Amount { get; set; }

    /// <summary>The year this amount is in force for; updated annually with the revaluation rate.</summary>
    public int ValidityYear { get; set; }
}
