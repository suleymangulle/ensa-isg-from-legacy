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

        builder.HasIndex(x => new { x.CompanyEmployeeId, x.TrainingId, x.TrainingTopicId })
               .IsUnique();

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.TrainingId);
        builder.HasIndex(x => x.TrainingTopicId);
        builder.HasIndex(x => x.TrainingSpecialistUserId);
        builder.HasIndex(x => x.TrainingPhysicianUserId);
    }
}
