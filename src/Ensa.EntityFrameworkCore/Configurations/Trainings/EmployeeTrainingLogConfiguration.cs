using Ensa.Domain.Trainings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary>
/// <see cref="EmployeeTrainingLog"/> table mapping.
/// <para>An append-only audit table; the indexes follow the read scenarios.</para>
/// </summary>
public class EmployeeTrainingLogConfiguration : IEntityTypeConfiguration<EmployeeTrainingLog>
{
    public void Configure(EntityTypeBuilder<EmployeeTrainingLog> builder)
    {
        builder.ToTable("EmployeeTrainingLog");
        builder.HasKey(x => x.Id);

        // Time-ordered transaction listing for an employee.
        builder.HasIndex(x => new { x.CompanyEmployeeId, x.CreationTime });

        // Statistics and filtering by transaction type.
        builder.HasIndex(x => x.Operation);

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.TrainingTopicId);
        builder.HasIndex(x => x.ExamId);
        builder.HasIndex(x => x.EmployeeTrainingProgressId);
    }
}
