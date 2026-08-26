using Ensa.Domain.Trainings;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary><see cref="EmployeeExamAnswer"/> table mapping.</summary>
public class EmployeeExamAnswerConfiguration : IEntityTypeConfiguration<EmployeeExamAnswer>
{
    public void Configure(EntityTypeBuilder<EmployeeExamAnswer> builder)
    {
        builder.ToTable("EmployeeExamAnswer");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Answer)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        // Exam evaluation: the answer an employee gave to a specific question.
        builder.HasIndex(x => new { x.CompanyEmployeeId, x.ExamQuestionId });

        // Fetching every answer belonging to one progress record.
        builder.HasIndex(x => x.EmployeeTrainingProgressId);

        builder.HasIndex(x => x.ExamQuestionId);
    }
}
