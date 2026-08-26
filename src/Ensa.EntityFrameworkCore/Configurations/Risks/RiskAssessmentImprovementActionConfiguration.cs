using Ensa.Domain.Risks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>The "improvement suggestions" ticked on the report.</summary>
public class RiskAssessmentImprovementActionConfiguration : IEntityTypeConfiguration<RiskAssessmentImprovementAction>
{
    public void Configure(EntityTypeBuilder<RiskAssessmentImprovementAction> builder)
    {
        builder.ToTable("RiskAssessmentImprovementAction");
        builder.HasKey(x => x.Id);

        // At most one row per improvement suggestion per report.
        builder.HasIndex(x => new { x.TenantId, x.RiskAssessmentReportId, x.Recommendation })
               .IsUnique();

        builder.HasIndex(x => x.RiskAssessmentReportId);
    }
}
