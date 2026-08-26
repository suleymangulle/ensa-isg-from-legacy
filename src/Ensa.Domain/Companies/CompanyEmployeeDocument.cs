using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Companies;

/// <summary>
/// Link between an employee and a document — a training attendance certificate, a certificate,
/// a health report, and so on. It also carries the IBYS (the national OHS information management
/// system) submission status.
/// <para>Legacy equivalent: <c>CompanyEmployeeDocument_T</c>.</para>
/// <para>
/// NORMALISATION: the legacy <c>RiskAssessmentTeamDocument</c>, <c>EmergencyTeamDocument</c> and
/// <c>OhsCommitteeDocument</c> boolean trio was reduced to the single
/// <see cref="TeamDocumentType"/> enum field.
/// </para>
/// </summary>
public class CompanyEmployeeDocument : FullAuditedTenantEntity, IActivatable
{
    public int CompanyEmployeeId { get; set; }

    /// <summary>FK to the central <c>Document</c> table.</summary>
    public int DocumentId { get; set; }

    /// <summary>Date the document was issued / its validity starts.</summary>
    public DateTime? DocumentDate { get; set; }

    // ---------------- Source links ----------------

    /// <summary>The training definition, when the document belongs to a training.</summary>
    public int? TrainingId { get; set; }

    /// <summary>The training plan line that produced the document.</summary>
    public int? TrainingPlanLineId { get; set; }

    /// <summary>The work plan line that produced the document.</summary>
    public int? WorkPlanLineId { get; set; }

    /// <summary>FK to the host <c>CertificateList</c> definition, when the document is a certificate.</summary>
    public int? CertificateId { get; set; }

    /// <summary>Free-text name of a certificate that is not in the certificate list.</summary>
    public string? OtherCertificateName { get; set; }

    /// <summary>Which workplace team the document belongs to. (Legacy: 3 separate bool columns)</summary>
    public EmployeeTeamDocumentType TeamDocumentType { get; set; } = EmployeeTeamDocumentType.None;

    /// <summary>Group code that marks records belonging to the same batch operation.</summary>
    public string? GroupCode { get; set; }

    /// <summary>Which screen/operation created the record (legacy tracing text).</summary>
    public string? Source { get; set; }

    // ---------------- IBYS ----------------

    /// <summary>IBYS submission status. (Legacy: <c>IBYSDurum</c>, a string "-1"/"0"/"1")</summary>
    public IbysSubmissionStatus IbysStatus { get; set; } = IbysSubmissionStatus.NotSent;

    /// <summary>Number of submission attempts made.</summary>
    public int? IbysSubmissionAttempt { get; set; }

    /// <summary>Status code returned by the IBYS service.</summary>
    public string? IbysStatusCode { get; set; }

    /// <summary>Message/error text returned by the IBYS service.</summary>
    public string? IbysMessage { get; set; }

    /// <summary>IBYS submission number.</summary>
    public string? IbysNotificationNo { get; set; }

    /// <summary>The IBYS query record that tracks the submission.</summary>
    public int? IbysQueryId { get; set; }

    /// <summary>(Legacy: <c>Aktif</c> bool?)</summary>
    public bool IsActive { get; set; } = true;
}
