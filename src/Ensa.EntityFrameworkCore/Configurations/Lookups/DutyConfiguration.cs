using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="Duty"/> table mapping.
/// <para>A host (tenant-less) reference table.</para>
/// </summary>
public class DutyConfiguration : IEntityTypeConfiguration<Duty>
{
    public void Configure(EntityTypeBuilder<Duty> builder)
    {
        builder.ToTable("Duty");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DutyCode)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.DutyName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.DutyLabel)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.ShortName);

        builder.HasIndex(x => x.DutyCode).IsUnique();
    }
}
