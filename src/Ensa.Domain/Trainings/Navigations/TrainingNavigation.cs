using System.ComponentModel.DataAnnotations.Schema;
using Ensa.Domain.Common;

namespace Ensa.Domain.Trainings.Navigations;

/// <summary>
/// Combined view of a training with its group, per-hazard-class durations, topics (including their
/// own durations) and linked exams.
/// <para>
/// <c>[NotMapped]</c> — NEVER a <c>DbSet</c>, never added to <c>ModelBuilder</c>;
/// populated in the repository layer through an <c>IQueryable</c> join and projection.
/// </para>
/// </summary>
[NotMapped]
public class TrainingNavigation : NavigationEntity<Training>
{
    /// <summary>Shortcut to the root record (the same object as <see cref="NavigationEntity{TEntity}.Entity"/>).</summary>
    public Training Training
    {
        get => Entity;
        set => Entity = value;
    }

    public TrainingGroup? TrainingGroup { get; set; }

    /// <summary>Mandatory durations per hazard class. (Legacy: the triple of duration columns)</summary>
    public List<TrainingDuration> Durations { get; set; } = [];

    /// <summary>The training's topics, each with its own per-hazard-class durations.</summary>
    public List<TrainingTopicNavigation> Subjects { get; set; } = [];

    /// <summary>The exams linked to the training.</summary>
    public List<Exam> Exams { get; set; } = [];
}

/// <summary>Combined view of a training topic and its per-hazard-class durations.</summary>
[NotMapped]
public class TrainingTopicNavigation : NavigationEntity<TrainingTopic>
{
    public TrainingTopic TrainingTopic
    {
        get => Entity;
        set => Entity = value;
    }

    public List<TrainingTopicDuration> Durations { get; set; } = [];
}
