using Ensa.Domain.Common;

namespace Ensa.Domain.Trainings;

/// <summary>
/// A single topic within a training — one distance-learning presentation.
/// <para>Legacy equivalent: <c>TrainingTopic_T</c> (PK: <c>TopicId</c>).</para>
/// <para>
/// NORMALIZATION: the legacy
/// <c>AzHazardousDuration</c>/<c>HazardousDuration</c>/<c>VeryHazardousDuration</c> columns were
/// removed and moved into the <see cref="TrainingTopicDuration"/> child table.
/// </para>
/// </summary>
public class TrainingTopic : FullAuditedTenantEntity
{
    public int TrainingId { get; set; }

    public string TopicTitle { get; set; } = string.Empty;

    /// <summary>Address or path of the distance-learning presentation file.</summary>
    public string? PresentationAddress { get; set; }

    public int PresentationPageCount { get; set; }

    /// <summary>Display order within the training.</summary>
    public int TopicOrder { get; set; }
}
