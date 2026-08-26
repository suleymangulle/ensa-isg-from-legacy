using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>
/// SKRS medication frequency unit code list.
/// <para>Mapped to its own table (no TPT/TPH). A host table.</para>
/// </summary>
public class MedicationFrequencyUnitConfiguration : IEntityTypeConfiguration<MedicationFrequencyUnit>
{
    public void Configure(EntityTypeBuilder<MedicationFrequencyUnit> builder)
    {
        builder.ToTable("MedicationFrequencyUnit");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodeTypeName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => x.Code)
               .IsUnique()
               .HasFilter("[Code] IS NOT NULL");
    }
}
