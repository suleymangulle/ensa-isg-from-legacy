using Ensa.Domain.Reports;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Reports;

/// <summary><see cref="ActivityReportLine"/> table mapping.</summary>
public class ActivityReportLineConfiguration : IEntityTypeConfiguration<ActivityReportLine>
{
    public void Configure(EntityTypeBuilder<ActivityReportLine> builder)
    {
        builder.ToTable("ActivityReportLine");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Value1)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.Value2)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.Value3)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        // Reading report lines in a stable order (also served by the foreign key index).
        builder.HasIndex(x => new { x.ActivityReportId, x.OrderNo });
    }
}
