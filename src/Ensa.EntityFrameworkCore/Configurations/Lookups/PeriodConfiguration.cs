using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="Period"/> table mapping.
/// <para>A host (tenant-less) reference table.</para>
/// </summary>
public class PeriodConfiguration : IEntityTypeConfiguration<Period>
{
    public void Configure(EntityTypeBuilder<Period> builder)
    {
        builder.ToTable("Period");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PeriodName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.PeriodExpression)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // The unit + value pair is the natural key of a period.
        builder.HasIndex(x => new { x.PeriodUnit, x.PeriodValue });
    }
}
