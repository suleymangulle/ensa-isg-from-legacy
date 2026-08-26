using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="OccupationCode"/> table mapping (NACE code reference).
/// <para>A host (tenant-less) reference table.</para>
/// </summary>
public class OccupationCodeConfiguration : IEntityTypeConfiguration<OccupationCode>
{
    public void Configure(EntityTypeBuilder<OccupationCode> builder)
    {
        builder.ToTable("OccupationCode");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NaceCode)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Tag)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        // Resolving the hazard class from a NACE code.
        // Not UNIQUE, because the legacy data may contain duplicate codes.
        builder.HasIndex(x => x.NaceCode);
    }
}
