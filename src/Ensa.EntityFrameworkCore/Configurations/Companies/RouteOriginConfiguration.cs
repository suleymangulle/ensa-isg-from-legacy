using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="RouteOrigin"/> table mapping (starting point of a visit route).
/// </summary>
public class RouteOriginConfiguration : IEntityTypeConfiguration<RouteOrigin>
{
    public void Configure(EntityTypeBuilder<RouteOrigin> builder)
    {
        builder.ToTable("RouteOrigin");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Tag)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Address)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Address);

        builder.HasIndex(x => x.CityId);
        builder.HasIndex(x => x.DistrictId);
    }
}
