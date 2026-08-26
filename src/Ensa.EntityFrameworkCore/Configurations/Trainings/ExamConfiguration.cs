using Ensa.Domain.Trainings;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Trainings;

/// <summary><see cref="Exam"/> table mapping.</summary>
public class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.ToTable("Exam");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}
