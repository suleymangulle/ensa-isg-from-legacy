using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>Medication line of an e-prescription.</summary>
public class EPrescriptionMedicationConfiguration : IEntityTypeConfiguration<EPrescriptionMedication>
{
    public void Configure(EntityTypeBuilder<EPrescriptionMedication> builder)
    {
        builder.ToTable("EPrescriptionMedication");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MedicationBarcode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.MedicationDescription!)
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Text);

        // Fractional dose (e.g. 0.5 tablets).
        builder.Property(x => x.DoseFraction).HasPrecision(9, 3);

        builder.HasIndex(x => x.EPrescriptionId);
        builder.HasIndex(x => x.MedicationId);
        builder.HasIndex(x => x.UsageMethodId);
        builder.HasIndex(x => x.UsageDoseUnitId);
        builder.HasIndex(x => x.UsagePeriodUnitId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
