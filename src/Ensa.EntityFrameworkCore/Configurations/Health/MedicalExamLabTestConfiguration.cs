using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>Laboratory / diagnostic test line on an examination form.</summary>
public class MedicalExamLabTestConfiguration : IEntityTypeConfiguration<MedicalExamLabTest>
{
    public void Configure(EntityTypeBuilder<MedicalExamLabTest> builder)
    {
        builder.ToTable("MedicalExamLabTest");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Result!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Text);

        // At most one row per lab test type per form.
        builder.HasIndex(x => new { x.TenantId, x.MedicalExaminationFormId, x.LabTestType })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.MedicalExaminationFormId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
