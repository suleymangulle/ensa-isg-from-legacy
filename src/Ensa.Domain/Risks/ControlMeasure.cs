using Ensa.Domain.Common;

namespace Ensa.Domain.Risks;

/// <summary>
/// A single control measure taken, or to be taken, for an identified hazard.
/// (Legacy: <c>RiskAnalizRaporuAlinanOnlem_T</c>)
/// <para>
/// Legacy stored only the <c>ControlMeasure</c> text, so a measure could not be followed up — no
/// owner, no deadline, no completion. To satisfy the "follow-up of measures" requirement of Law
/// No. 6331, the <see cref="DeadlineDate"/>, <see cref="OwnerCompanyEmployeeId"/>,
/// <see cref="IsCompleted"/> and <see cref="CompletionDate"/> fields were added.
/// </para>
/// </summary>
public class ControlMeasure : FullAuditedTenantEntity
{
    /// <summary>FK → <see cref="IdentifiedHazard"/>.</summary>
    public int IdentifiedHazardId { get; set; }

    /// <summary>Textual description of the measure. (Legacy: <c>AlinanOnlem</c>)</summary>
    public string Measure { get; set; } = string.Empty;

    /// <summary>The date by which the measure must be completed. (Not present in legacy)</summary>
    public DateTime? DeadlineDate { get; set; }

    /// <summary>The company employee responsible for the measure. FK → <c>CompanyEmployee.Id</c>. (Not present in legacy)</summary>
    public int? OwnerCompanyEmployeeId { get; set; }

    /// <summary>Whether the measure has been completed. (Not present in legacy)</summary>
    public bool IsCompleted { get; set; }

    /// <summary>The date the measure was actually completed. (Not present in legacy)</summary>
    public DateTime? CompletionDate { get; set; }
}
