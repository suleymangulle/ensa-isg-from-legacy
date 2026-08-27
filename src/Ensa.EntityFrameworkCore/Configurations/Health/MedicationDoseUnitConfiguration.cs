using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>
/// SKRS medication dose unit code list.
/// <para>Mapped to its own table (no TPT/TPH). A host table.</para>
/// </summary>
public class MedicationDoseUnitConfiguration : IEntityTypeConfiguration<MedicationDoseUnit>
{
    public void Configure(EntityTypeBuilder<MedicationDoseUnit> builder)
    {
        builder.ToTable("MedicationDoseUnit");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodeTypeName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // NOT unique. The legacy list holds two overlapping ministry code sets under one type
        // name: codes 1 to 5 appear twice, once as "MCG/KG/DAK, GRAM, MIKROGRAM..." and once as
        // "Adet, Mililitre, Miligram...". Prescriptions reference rows from both sets by row id,
        // so neither can be dropped, and the code was never the key here.
        builder.HasIndex(x => x.Code)
               .HasFilter("[Code] IS NOT NULL");
    }
}
