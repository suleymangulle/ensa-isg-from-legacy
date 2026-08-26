using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>
/// SKRS medication route code list.
/// <para>
/// Derives from the abstract <c>SkrsReferenceEntity</c> base; this is <b>not TPT/TPH</b>, it is mapped to its
/// own table (the base class never enters the model and has no configuration).
/// A host table.
/// </para>
/// </summary>
public class MedicationRouteConfiguration : IEntityTypeConfiguration<MedicationRoute>
{
    public void Configure(EntityTypeBuilder<MedicationRoute> builder)
    {
        builder.ToTable("MedicationRoute");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodeTypeName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // The SKRS code is nullable; filtered unique index.
        builder.HasIndex(x => x.Code)
               .IsUnique()
               .HasFilter("[Code] IS NOT NULL");
    }
}
