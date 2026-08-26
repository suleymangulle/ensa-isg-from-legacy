using Ensa.Domain.Common;

namespace Ensa.Domain.Companies;

/// <summary>
/// Numeric summary of a company's outstanding obligations.
/// <para>Legacy equivalent: <c>CompanyUyarilar_T</c>.</para>
/// <para>
/// NOTE — this is a <b>denormalised summary (cache) table</b>. The values are derived from
/// records in the training, health and equipment modules and are <b>recomputed periodically by a
/// background job</b>. It is not source data: business rule validation never reads from it, and
/// it exists only for list and dashboard rendering. There is at most one row per company.
/// </para>
/// </summary>
public class CompanyComplianceSummary : AuditedTenantEntity, ICompanyScoped
{
    /// <summary>The company the summary belongs to. Unique FK.</summary>
    public int CompanyId { get; set; }

    /// <summary>Number of employees who have had no occupational safety training at all.</summary>
    public int? IsSafetyTrainingNoneCount { get; set; }

    /// <summary>Number of employees whose occupational safety training is incomplete or expired.</summary>
    public int? IsSafetyTrainingMissingCount { get; set; }

    /// <summary>Number of employees who have had no occupational health training at all.</summary>
    public int? IsHealthTrainingNoneCount { get; set; }

    /// <summary>Number of employees whose occupational health training is incomplete or expired.</summary>
    public int? IsHealthTrainingMissingCount { get; set; }

    /// <summary>Number of employees missing a pre-employment health examination.</summary>
    public int? PreEmploymentHealthExaminationMissingCount { get; set; }

    /// <summary>Number of equipment items with a missing or overdue periodic inspection.</summary>
    public int? EquipmentExaminationMissingCount { get; set; }

    /// <summary>When the summary was last computed (written by the background job).</summary>
    public DateTime? CalculatedTime { get; set; }
}
