using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Risks;

/// <summary>
/// A single identified hazard line on a risk assessment report.
/// (Legacy: <c>RiskAnalizRaporuBelirlenenTehlike_T</c>)
/// <para>
/// Conversions: the <c>double</c> score fields became <c>decimal</c>; the <c>Source</c> string
/// column became the <see cref="SourceType"/> enum; the
/// <c>[NotMapped] Document/DocumentName/DocumentType</c> fields were removed in favour of the
/// <see cref="DocumentId"/> FK.
/// </para>
/// </summary>
public class IdentifiedHazard : FullAuditedTenantEntity
{
    /// <summary>FK → <see cref="RiskAssessmentReport"/>.</summary>
    public int RiskAssessmentReportId { get; set; }

    /// <summary>FK → <see cref="HazardCategory"/>. <c>null</c> when it was not picked from the library.</summary>
    public int? HazardCategoryId { get; set; }

    /// <summary>FK → <see cref="Hazard"/>, the hazard library. <c>null</c> when entered by hand.</summary>
    public int? HazardId { get; set; }

    /// <summary>Textual description of the hazard, copied from the library or typed in.</summary>
    public string HazardTag { get; set; } = string.Empty;

    /// <summary>The activity or work step where the hazard arises.</summary>
    public string? ActivityDescription { get; set; }

    /// <summary>The person responsible for the hazard, as free text. (Legacy: <c>SorumluKisi</c>)</summary>
    public string? OwnerPerson { get; set; }

    /// <summary>Description of the risk the hazard gives rise to. (Legacy: <c>Risk</c>)</summary>
    public string? RiskTag { get; set; }

    /// <summary>Summary of the proposed or applied control measure. (Legacy: <c>Onlem</c>)</summary>
    public string? Measure { get; set; }

    // ---- Assessment BEFORE controls ----

    /// <summary>Likelihood value. (Legacy: <c>double Olasilik</c> → <c>decimal</c>)</summary>
    public decimal Likelihood { get; set; }

    /// <summary>Severity — the degree of harm. (Legacy: <c>double Siddet</c>)</summary>
    public decimal Severity { get; set; }

    /// <summary>Frequency of exposure. Used by the Fine-Kinney method only.</summary>
    public decimal Frequency { get; set; }

    /// <summary>
    /// Risk score before controls.
    /// <para>
    /// COMPUTED FIELD: <c>IRiskAssessmentManager.CalculateAsync</c> computes it according to the
    /// method (L-Matrix: L×S, Fine-Kinney: L×F×S) and writes it to this column. The entity performs
    /// no calculation of its own and the field must not be set directly.
    /// Legacy had no such column and recomputed the score on every screen; it was made persistent
    /// for reporting and sorting performance.
    /// </para>
    /// </summary>
    public decimal RiskScore { get; set; }

    /// <summary>Assessment comment. (Legacy: <c>Yorum</c>)</summary>
    public string? Comment { get; set; }

    // ---- Assessment AFTER controls (legacy TS* columns) ----

    /// <summary>Residual likelihood, after controls. (Legacy: <c>TSOlasilik</c>)</summary>
    public decimal? ResidualLikelihood { get; set; }

    /// <summary>Residual severity, after controls. (Legacy: <c>TSSiddet</c>)</summary>
    public decimal? ResidualSeverity { get; set; }

    /// <summary>Residual frequency, after controls. (Legacy: <c>TSFrekans</c>)</summary>
    public decimal? ResidualFrequency { get; set; }

    /// <summary>
    /// Residual risk score, after controls.
    /// <para>
    /// COMPUTED FIELD: <c>IRiskAssessmentManager</c> writes it once every residual value has been
    /// entered; otherwise it stays <c>null</c>.
    /// </para>
    /// </summary>
    public decimal? ResidualRiskScore { get; set; }

    /// <summary>Residual assessment comment. (Legacy: <c>TSYorum</c>)</summary>
    public string? ResidualComment { get; set; }

    // ---- Source tracing ----

    /// <summary>
    /// Where this hazard line originated. (Legacy: the <c>Kaynak</c> string column)
    /// </summary>
    public HazardSourceType SourceType { get; set; } = HazardSourceType.Manual;

    /// <summary>
    /// Id of the source record; its meaning depends on <see cref="SourceType"/> — a hazard, a field
    /// observation line, a corrective action or an incident. (Legacy: <c>KaynakId</c>)
    /// </summary>
    public int? SourceId { get; set; }

    /// <summary>Supporting document or photograph. FK → <c>Document.Id</c>.</summary>
    public int? DocumentId { get; set; }

    /// <summary>The deadline given for completing the control measures.</summary>
    public DateTime? DeadlineDate { get; set; }
}
