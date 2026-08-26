using Ensa.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Reports;

/// <summary>
/// <see cref="OhsReportHazardClassBreakdown"/> table mapping.
/// <para>Uniqueness is on (<c>OhsReportId</c>, <c>HazardClass</c>).</para>
/// </summary>
public class OhsReportHazardClassBreakdownConfiguration
    : IEntityTypeConfiguration<OhsReportHazardClassBreakdown>
{
    public void Configure(EntityTypeBuilder<OhsReportHazardClassBreakdown> builder)
    {
        builder.ToTable("OhsReportHazardClassBreakdown");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.OhsReportId, x.HazardClass })
               .IsUnique();
    }
}
