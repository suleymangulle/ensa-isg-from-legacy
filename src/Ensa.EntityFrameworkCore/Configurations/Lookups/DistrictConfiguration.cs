using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="District"/> table mapping.
/// <para>A host (tenant-less) reference table.</para>
/// </summary>
public class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("District");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DistrictName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // District list for a selected city — the most frequently used query.
        builder.HasIndex(x => new { x.CityId, x.DistrictName });

        builder.HasIndex(x => x.CityId);
    }
}
