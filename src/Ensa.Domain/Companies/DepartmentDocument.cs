using Ensa.Domain.Common;

namespace Ensa.Domain.Companies;

/// <summary>
/// Document belonging to a workplace department (measurement report, inspection certificate,
/// licence, and so on).
/// <para>Legacy equivalent: <c>WorkplaceDepartmentDocument_T</c>.</para>
/// </summary>
public class DepartmentDocument : FullAuditedTenantEntity
{
    /// <summary>The department it belongs to. (Legacy: <c>BolumId</c>)</summary>
    public int WorkplaceDepartmentId { get; set; }

    /// <summary>Document type code (legacy free text).</summary>
    public string? DocumentCode { get; set; }

    public string? Description { get; set; }

    /// <summary>FK to the central <c>Document</c> table.</summary>
    public int? DocumentId { get; set; }

    /// <summary>Date of the measurement/inspection.</summary>
    public DateTime? ExaminationDate { get; set; }

    /// <summary>Date the document expires.</summary>
    public DateTime? ValidityDate { get; set; }

    /// <summary>Person or organisation that performed the measurement/inspection (legacy free text).</summary>
    public string? ExaminationPerformedBy { get; set; }

    /// <summary>The activity definition that makes the document mandatory.</summary>
    public int? ActivityId { get; set; }

    /// <summary>The work plan line the document was produced from.</summary>
    public int? WorkPlanLineId { get; set; }
}
