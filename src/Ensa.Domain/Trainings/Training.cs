using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Trainings;

/// <summary>
/// A training definition (catalogue entry) — the template assigned to employees on the
/// distance-learning platform.
/// <para>Legacy equivalent: <c>Training_T</c>.</para>
/// <para>
/// The <c>TenantId</c> on the <see cref="Entity{TKey}"/> base class corresponds to the nullable
/// legacy <c>OrganizationId</c> column. When <c>TenantId == null</c>, the training is a shared
/// template available to every organization.
/// </para>
/// <para>
/// NORMALIZATION: the legacy
/// <c>Duration</c>/<c>AzHazardousDuration</c>/<c>HazardousDuration</c>/<c>VeryHazardousDuration</c>
/// columns were removed from the header and moved into the <see cref="TrainingDuration"/> child
/// table. The legacy <c>GeneralSubjects</c>/<c>HealthSubjects</c>/<c>TechnicalSubjects</c> boolean
/// triple was collapsed into the <see cref="TopicGroup"/> enum field.
/// </para>
/// </summary>
public class Training : FullAuditedTenantEntity, IActivatable
{
    public string TrainingName { get; set; } = string.Empty;

    public string? TrainingCode { get; set; }

    /// <summary>The training group (category) this training belongs to.</summary>
    public int? TrainingGroupId { get; set; }

    /// <summary>(Legacy: <c>Tur</c> string)</summary>
    public TrainingType TrainingType { get; set; } = TrainingType.BasicTraining;

    /// <summary>
    /// Subject group of the training. (Legacy: the
    /// <c>GenelKonular</c>/<c>HealthSubjects</c>/<c>TechnicalSubjects</c> boolean triple, normalized.)
    /// </summary>
    public TrainingSubjectGroup TopicGroup { get; set; } = TrainingSubjectGroup.GeneralSubjects;

    public bool MandatoryTraining { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>İSG-KATİP (İBYS) training code. (Legacy: <c>IBYS_EgitimKodu</c>)</summary>
    public int? IbysTrainingCode { get; set; }

    /// <summary>
    /// Whether the training is included in a training plan by default when the plan is created.
    /// (Legacy: <c>DefaultPlan</c>)
    /// </summary>
    public bool IncludedInDefaultPlan { get; set; }

    /// <summary>Whether the training belongs to a company's default, mandatory training list. (Legacy: <c>DefaultEgitim</c>)</summary>
    public bool DefaultTraining { get; set; }

    /// <summary>The occurrence count used when generating a default plan. (Legacy: <c>DefaultAdet</c>)</summary>
    public int DefaultCount { get; set; }

    /// <summary>Month offset relative to the start of the plan. (Legacy: <c>DefaultBaslangicAyKaydirma</c>)</summary>
    public int DefaultStartMonthOffset { get; set; }

    /// <summary>Minimum employee count at which this training becomes mandatory. (Legacy: <c>DefaultElemanSarti</c>)</summary>
    public int DefaultElementCondition { get; set; }
}
