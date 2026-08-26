using Ensa.Domain.Risks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>The "groups exposed to the hazard" ticked on the report.</summary>
public class RiskAssessmentExposedGroupConfiguration : IEntityTypeConfiguration<RiskAssessmentExposedGroup>
{
    public void Configure(EntityTypeBuilder<RiskAssessmentExposedGroup> builder)
    {
        builder.ToTable("RiskAssessmentExposedGroup");
        builder.HasKey(x => x.Id);

        // At most one row per group per report.
        builder.HasIndex(x => new { x.TenantId, x.RiskAssessmentReportId, x.Group })
               .IsUnique();

        builder.HasIndex(x => x.RiskAssessmentReportId);
    }
}
