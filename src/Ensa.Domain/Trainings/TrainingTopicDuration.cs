using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Trainings;

/// <summary>
/// A training topic's duration in minutes, per workplace hazard class.
/// <para>
/// NORMALIZATION: this replaces the flat legacy
/// <c>TrainingTopic_T.AzHazardousDuration</c>/<c>HazardousDuration</c>/<c>VeryHazardousDuration</c>
/// columns. Unique on (<see cref="TrainingTopicId"/>, <see cref="HazardClass"/>).
/// </para>
/// </summary>
public class TrainingTopicDuration : CreationAuditedTenantEntity
{
    public int TrainingTopicId { get; set; }

    public HazardClass HazardClass { get; set; }

    public int DurationMinutes { get; set; }
}
