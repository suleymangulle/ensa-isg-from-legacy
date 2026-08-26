using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>
/// SKRS medication reference.
/// <para>A host table (it does not implement <c>IMultiTenant</c>).</para>
/// </summary>
public class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.ToTable("Medication");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MedicationName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.Barcode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.GeneratorCompanyName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.AtcCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.AtcName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.OutpatientReimbursementCondition)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.InpatientReimbursementCondition)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.PrescriptionType)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.ShortName);

        // Barcode is nullable; SQL Server allows only a single NULL, so a filtered unique index is used.
        builder.HasIndex(x => x.Barcode)
               .IsUnique()
               .HasFilter("[Barcode] IS NOT NULL");

        builder.HasIndex(x => x.MedicationName);
        builder.HasIndex(x => x.AtcCode);
    }
}
