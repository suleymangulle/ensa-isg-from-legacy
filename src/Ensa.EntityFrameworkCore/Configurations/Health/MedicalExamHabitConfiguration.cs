using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>Habit line (smoking / alcohol / substance) on an examination form.</summary>
public class MedicalExamHabitConfiguration : IEntityTypeConfiguration<MedicalExamHabit>
{
    public void Configure(EntityTypeBuilder<MedicalExamHabit> builder)
    {
        builder.ToTable("MedicalExamHabit");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Text);

        // At most one row per habit type per form.
        builder.HasIndex(x => new { x.TenantId, x.MedicalExaminationFormId, x.HabitType })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.MedicalExaminationFormId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
