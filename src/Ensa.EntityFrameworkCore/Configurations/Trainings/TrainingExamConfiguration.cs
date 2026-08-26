using Ensa.Domain.Trainings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary>
/// <see cref="TrainingExam"/> (link table between a training and an exam) mapping.
/// <para>The same exam can be linked to the same training only once.</para>
/// </summary>
public class TrainingExamConfiguration : IEntityTypeConfiguration<TrainingExam>
{
    public void Configure(EntityTypeBuilder<TrainingExam> builder)
    {
        builder.ToTable("TrainingExam");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.TrainingId, x.ExamId })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.ExamId);
    }
}
