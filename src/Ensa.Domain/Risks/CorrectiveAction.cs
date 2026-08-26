using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// A corrective and preventive action record (DÖF). (Legacy: <c>DOF_T</c>)
/// <para>
/// Conversions: <c>OperationResult int</c> (0/1/-1) → <see cref="CorrectiveActionStatus"/>;
/// <c>Risk</c> string → <see cref="RiskCategory"/>; <c>TDocumentId</c>/<c>SDocumentID</c> →
/// <see cref="FindingDocumentId"/>/<see cref="ResultDocumentId"/>; the
/// <see cref="OwnerCompanyEmployeeId"/> FK was added alongside the <c>Owner</c> string.
/// </para>
/// </summary>
public class CorrectiveAction : FullAuditedTenantEntity, ICompanyScoped
{
    /// <summary>The company the corrective action was raised for. FK → <c>Company.Id</c>.</summary>
    public int CompanyId { get; set; }

    /// <summary>Uygunsuzluk tespiti. (Legacy: <c>Tespit</c>)</summary>
    public string Finding { get; set; } = string.Empty;

    /// <summary>The proposed corrective or preventive action. (Legacy: <c>Oneri</c>)</summary>
    public string? Recommendation { get; set; }

    /// <summary>Faaliyet sonucu. (Legacy: <c>Sonuc</c>)</summary>
    public string? Result { get; set; }

    /// <summary>Why the action was raised, or where it came from; free text. (Legacy: <c>Kaynak</c>)</summary>
    public string? Source { get; set; }

    /// <summary>Document for the finding stage. FK → <c>Document.Id</c>. (Legacy: <c>TDosyaId</c>)</summary>
    public int? FindingDocumentId { get; set; }

    /// <summary>Document for the closing stage. FK → <c>Document.Id</c>. (Legacy: <c>SDosyaID</c>)</summary>
    public int? ResultDocumentId { get; set; }

    /// <summary>Risk category of the finding. (Legacy: <c>Risk</c> string)</summary>
    public RiskCategory RiskCategory { get; set; }

    /// <summary>Workflow status of the action. (Legacy: <c>IslemSonucu</c> — 0: in progress, 1: closed, -1: cancelled)</summary>
    public CorrectiveActionStatus OperationResult { get; set; } = CorrectiveActionStatus.InProgress;

    /// <summary>Name of the responsible person, free text. (Legacy: <c>Sorumlu</c>)</summary>
    public string? Owner { get; set; }

    /// <summary>
    /// The company employee responsible. FK → <c>CompanyEmployee.Id</c>.
    /// Legacy held free text only; the FK was added for follow-up and reporting.
    /// </summary>
    public int? OwnerCompanyEmployeeId { get; set; }

    /// <summary>The date the non-conformity was identified.</summary>
    public DateTime? FindingDate { get; set; }

    /// <summary>The date by which the action must be completed.</summary>
    public DateTime? DeadlineDate { get; set; }

    /// <summary>The date the action was concluded.</summary>
    public DateTime? ResultDate { get; set; }

    /// <summary>
    /// The source line, when the action was derived from a field observation line.
    /// FK → <see cref="FieldObservationLine"/>. (Legacy: <c>SahaGozlemSatiriId</c>)
    /// </summary>
    public int? FieldObservationLineId { get; set; }
}
