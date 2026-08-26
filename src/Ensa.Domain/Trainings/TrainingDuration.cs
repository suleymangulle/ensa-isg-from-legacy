using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Trainings;

/// <summary>
/// A training's mandatory duration in minutes, per workplace hazard class.
/// <para>
/// NORMALIZATION: this replaces the flat legacy
/// <c>Training_T.AzHazardousDuration</c>/<c>HazardousDuration</c>/<c>VeryHazardousDuration</c>
/// columns. Unique on (<see cref="TrainingId"/>, <see cref="HazardClass"/>).
/// </para>
/// </summary>
public class TrainingDuration : CreationAuditedTenantEntity
{
    public int TrainingId { get; set; }

    public HazardClass HazardClass { get; set; }

    public int DurationMinutes { get; set; }
}
