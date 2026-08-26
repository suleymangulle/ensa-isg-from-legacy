using Ensa.Domain.Risks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>The "existing protective measures" ticked on the report.</summary>
public class RiskAssessmentControlMeasureConfiguration : IEntityTypeConfiguration<RiskAssessmentControlMeasure>
{
    public void Configure(EntityTypeBuilder<RiskAssessmentControlMeasure> builder)
    {
        builder.ToTable("RiskAssessmentControlMeasure");
        builder.HasKey(x => x.Id);

        // At most one row per control measure per report.
        builder.HasIndex(x => new { x.TenantId, x.RiskAssessmentReportId, x.Measure })
               .IsUnique();

        builder.HasIndex(x => x.RiskAssessmentReportId);
    }
}
