using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="City"/> table mapping.
/// <para>A host (tenant-less) reference table — there is no TenantId column.</para>
/// </summary>
public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("City");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CityName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // The plate code (1-81) is unique; it is used as a natural key when matching addresses.
        builder.HasIndex(x => x.PlateCodeCode).IsUnique();

        builder.HasIndex(x => x.CityName);
    }
}
