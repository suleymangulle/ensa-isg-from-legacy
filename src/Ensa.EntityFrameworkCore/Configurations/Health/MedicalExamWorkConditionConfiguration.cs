using Ensa.Domain.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>Assessment line answering "is the employee fit to work under this condition?".</summary>
public class MedicalExamWorkConditionConfiguration : IEntityTypeConfiguration<MedicalExamWorkCondition>
{
    public void Configure(EntityTypeBuilder<MedicalExamWorkCondition> builder)
    {
        builder.ToTable("MedicalExamWorkCondition");
        builder.HasKey(x => x.Id);

        // At most one row per work condition type per form.
        builder.HasIndex(x => new { x.TenantId, x.MedicalExaminationFormId, x.ConditionType })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.MedicalExaminationFormId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
