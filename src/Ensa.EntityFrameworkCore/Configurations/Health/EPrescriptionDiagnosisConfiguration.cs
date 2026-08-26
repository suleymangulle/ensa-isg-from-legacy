using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>Diagnosis (ICD-10) line of an e-prescription.</summary>
public class EPrescriptionDiagnosisConfiguration : IEntityTypeConfiguration<EPrescriptionDiagnosis>
{
    public void Configure(EntityTypeBuilder<EPrescriptionDiagnosis> builder)
    {
        builder.ToTable("EPrescriptionDiagnosis");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Icd10Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.HasIndex(x => x.EPrescriptionId);
        builder.HasIndex(x => x.Icd10Id);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
