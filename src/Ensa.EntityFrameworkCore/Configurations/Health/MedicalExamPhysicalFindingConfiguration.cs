using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>Physical examination finding line on an examination form (per body system).</summary>
public class MedicalExamPhysicalFindingConfiguration : IEntityTypeConfiguration<MedicalExamPhysicalFinding>
{
    public void Configure(EntityTypeBuilder<MedicalExamPhysicalFinding> builder)
    {
        builder.ToTable("MedicalExamPhysicalFinding");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Text);

        // At most one row per body system per form.
        builder.HasIndex(x => new { x.TenantId, x.MedicalExaminationFormId, x.System })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.MedicalExaminationFormId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
