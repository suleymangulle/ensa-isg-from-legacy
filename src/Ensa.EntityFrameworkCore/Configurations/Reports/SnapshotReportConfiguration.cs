using Ensa.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Reports;

/// <summary>
/// <see cref="SnapshotReport"/> table mapping.
/// <para>
/// <see cref="SnapshotReport.JsonData"/> is a precomputed statistics output; its schema changes often, so it
/// is not normalised and is mapped as <c>nvarchar(max)</c>.
/// </para>
/// </summary>
public class SnapshotReportConfiguration : IEntityTypeConfiguration<SnapshotReport>
{
    public void Configure(EntityTypeBuilder<SnapshotReport> builder)
    {
        builder.ToTable("SnapshotReport");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.JsonData)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        // Snapshot reads: report type, office and date breakdown.
        builder.HasIndex(x => new { x.ReportType, x.OfficeId, x.ReportDate });

        // Foreign key index (no relationship is configured — index only).
        builder.HasIndex(x => x.OfficeId);
    }
}
