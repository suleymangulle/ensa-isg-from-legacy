using Ensa.Domain.Reports;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Reports;

/// <summary><see cref="ActivityReport"/> table mapping.</summary>
public class ActivityReportConfiguration : IEntityTypeConfiguration<ActivityReport>
{
    public void Configure(EntityTypeBuilder<ActivityReport> builder)
    {
        builder.ToTable("ActivityReport");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReportName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        // The company's periodic report list.
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.ReportStart });

        // Filtering by report type.
        builder.HasIndex(x => new { x.TenantId, x.ReportType });
    }
}
