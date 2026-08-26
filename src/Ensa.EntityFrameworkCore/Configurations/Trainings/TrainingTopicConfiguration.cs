using Ensa.Domain.Trainings;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary><see cref="TrainingTopic"/> table mapping.</summary>
public class TrainingTopicConfiguration : IEntityTypeConfiguration<TrainingTopic>
{
    public void Configure(EntityTypeBuilder<TrainingTopic> builder)
    {
        builder.ToTable("TrainingTopic");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TopicTitle)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.PresentationAddress)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Url);

        // Topics are always read in order within a training — this also serves the foreign key index.
        builder.HasIndex(x => new { x.TrainingId, x.TopicOrder });
    }
}
