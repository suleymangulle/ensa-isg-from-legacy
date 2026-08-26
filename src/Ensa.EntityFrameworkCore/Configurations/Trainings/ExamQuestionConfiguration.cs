using Ensa.Domain.Trainings;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary><see cref="ExamQuestion"/> table mapping.</summary>
public class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
{
    public void Configure(EntityTypeBuilder<ExamQuestion> builder)
    {
        builder.ToTable("ExamQuestion");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        // Legacy free-text correct-answer field; the source of truth is now ExamAnswer.
        builder.Property(x => x.CorrectAnswer)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.HasIndex(x => x.ExamId);
    }
}
