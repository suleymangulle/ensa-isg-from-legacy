using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Trainings.Dtos.Navigations;

/// <summary>
/// Combined view of a training: the catalogue entry, its group, its hazard-class durations,
/// its topics (each with their own durations) and the exams attached to it.
/// </summary>
public class TrainingNavigationDto : NavigationDto
{
    public TrainingDto Training { get; set; } = null!;

    /// <summary>Training group (category), reduced to a lookup.</summary>
    public LookupDto? TrainingGroup { get; set; }

    /// <summary>Topics in display order, each carrying its own hazard-class durations.</summary>
    public List<TrainingTopicDto> Topics { get; set; } = [];

    /// <summary>Exams attached to this training, reduced to lookups.</summary>
    public List<LookupDto> Exams { get; set; } = [];
}
