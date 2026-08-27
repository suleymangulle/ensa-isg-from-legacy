using Ensa.Domain.Trainings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary>
/// <see cref="EmployeeTrainingProgress"/> table mapping.
/// <para>
/// An employee can have only one progress record per training (and topic). Because
/// <c>TrainingTopicId</c> is nullable and SQL Server treats nulls as equal in a unique index, the
/// non-topic-based progress record is unique as well.
/// </para>
/// </summary>
public class EmployeeTrainingProgressConfiguration : IEntityTypeConfiguration<EmployeeTrainingProgress>
{
    public void Configure(EntityTypeBuilder<EmployeeTrainingProgress> builder)
    {
        builder.ToTable("EmployeeTrainingProgress");
        builder.HasKey(x => x.Id);

        // A worker's progress through one topic. NOT unique: the legacy system writes a new
        // progress row each time a worker retakes a topic rather than updating the existing one,
        // and 25 of the 6,500 migrated rows are retakes with genuinely different scores and
        // durations. Merging them would be worse than keeping them, because every exam answer
        // names the progress row it belongs to -- collapsing three attempts into one moves a
        // worker's answers onto an attempt they were not given for.
        builder.HasIndex(x => new { x.CompanyEmployeeId, x.TrainingId, x.TrainingTopicId });

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.TrainingId);
        builder.HasIndex(x => x.TrainingTopicId);
        builder.HasIndex(x => x.TrainingSpecialistUserId);
        builder.HasIndex(x => x.TrainingPhysicianUserId);
    }
}
