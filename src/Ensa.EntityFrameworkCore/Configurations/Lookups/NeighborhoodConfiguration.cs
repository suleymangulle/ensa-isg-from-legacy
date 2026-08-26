using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="Neighborhood"/> table mapping.
/// <para>A host (tenant-less) pure reference table; it carries no audit fields.</para>
/// </summary>
public class NeighborhoodConfiguration : IEntityTypeConfiguration<Neighborhood>
{
    public void Configure(EntityTypeBuilder<Neighborhood> builder)
    {
        builder.ToTable("Neighborhood");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NeighborhoodName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => new { x.DistrictId, x.NeighborhoodName });

        builder.HasIndex(x => x.DistrictId);
    }
}
