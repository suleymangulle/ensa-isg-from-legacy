using Ensa.Domain.Ibys;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Ibys;

/// <summary>
/// ISCO-08 occupation code.
/// <para>
/// A host table. The code is TEXT — it is not converted to an int, because leading zeros are significant.
/// </para>
/// </summary>
public class IbysIsco08OccupationCodeConfiguration : IEntityTypeConfiguration<IbysIsco08OccupationCode>
{
    public void Configure(EntityTypeBuilder<IbysIsco08OccupationCode> builder)
    {
        builder.ToTable("IbysIsco08OccupationCode");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
