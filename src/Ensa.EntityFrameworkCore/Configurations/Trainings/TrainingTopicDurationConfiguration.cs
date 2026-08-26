using Ensa.Domain.Trainings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary>
/// <see cref="TrainingTopicDuration"/> table mapping.
/// <para>Uniqueness is on (<c>TrainingTopicId</c>, <c>HazardClass</c>).</para>
/// </summary>
public class TrainingTopicDurationConfiguration : IEntityTypeConfiguration<TrainingTopicDuration>
{
    public void Configure(EntityTypeBuilder<TrainingTopicDuration> builder)
    {
        builder.ToTable("TrainingTopicDuration");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.TrainingTopicId, x.HazardClass })
               .IsUnique();
    }
}
