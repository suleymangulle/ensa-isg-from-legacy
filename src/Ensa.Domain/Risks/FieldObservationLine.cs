using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// A single non-conformity line on a field observation report.
/// (Legacy: <c>SahaGozlemRaporuSatirlari_T</c>)
/// <para>
/// Conversions: the <c>byte[] Document</c> + <c>DocumentName</c> + <c>DocumentType</c> triple was
/// removed and reduced to the <see cref="DocumentId"/> FK; <c>Risk</c> string →
/// <see cref="RiskCategory"/>; the <see cref="OwnerCompanyEmployeeId"/> FK was added alongside the
/// <c>Owner</c> string.
/// </para>
/// </summary>
public class FieldObservationLine : FullAuditedTenantEntity
{
    /// <summary>FK → <see cref="FieldObservationReport"/>.</summary>
    public int FieldObservationReportId { get; set; }

    /// <summary>The date the non-conformity was observed; it may differ from the report date.</summary>
    public DateTime? Date { get; set; }

    /// <summary>The deadline given to resolve it.</summary>
    public DateTime? DeadlineDate { get; set; }

    /// <summary>The non-conformity identified. (Legacy: <c>Uygunsuzluk</c>)</summary>
    public string NonConformity { get; set; } = string.Empty;

    /// <summary>The measures to be taken. (Legacy: <c>Onlemler</c>)</summary>
    public string? Measures { get; set; }

    /// <summary>Name of the responsible person, free text. (Legacy: <c>Sorumlu</c>)</summary>
    public string? Owner { get; set; }

    /// <summary>The company employee responsible. FK → <c>CompanyEmployee.Id</c>. (Not present in legacy)</summary>
    public int? OwnerCompanyEmployeeId { get; set; }

    /// <summary>Risk category of the non-conformity. (Legacy: <c>Risk</c> string)</summary>
    public RiskCategory RiskCategory { get; set; }

    /// <summary>Photograph or document evidencing the non-conformity. FK → <c>Document.Id</c>.</summary>
    public int? DocumentId { get; set; }
}
