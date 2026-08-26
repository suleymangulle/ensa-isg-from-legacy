using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="RouteOriginDistance"/> table mapping (distance cache).
/// </summary>
public class RouteOriginDistanceConfiguration : IEntityTypeConfiguration<RouteOriginDistance>
{
    public void Configure(EntityTypeBuilder<RouteOriginDistance> builder)
    {
        builder.ToTable("RouteOriginDistance");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CityName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // Distance is not money but is fractional; 9,2 is more than enough for kilometres.
        builder.Property(x => x.DistanceKm).HasPrecision(9, 2);

        // Cache key per origin. Rows with a null OriginId (computed from the city name)
        // are excluded from the uniqueness constraint.
        builder.HasIndex(x => new { x.OriginId, x.CompanyId })
               .IsUnique()
               .HasFilter("[OriginId] IS NOT NULL");

        builder.HasIndex(x => x.CompanyId);
    }
}
