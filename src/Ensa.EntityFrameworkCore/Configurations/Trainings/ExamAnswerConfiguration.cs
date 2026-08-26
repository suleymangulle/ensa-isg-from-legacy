using Ensa.Domain.Trainings;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary><see cref="ExamAnswer"/> table mapping.</summary>
public class ExamAnswerConfiguration : IEntityTypeConfiguration<ExamAnswer>
{
    public void Configure(EntityTypeBuilder<ExamAnswer> builder)
    {
        builder.ToTable("ExamAnswer");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AnswerText)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.HasIndex(x => x.ExamQuestionId);
    }
}
