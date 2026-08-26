using Ensa.Domain.Risks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>The "employee groups requiring a special policy" ticked on the report.</summary>
public class RiskAssessmentProtectedGroupConfiguration : IEntityTypeConfiguration<RiskAssessmentProtectedGroup>
{
    public void Configure(EntityTypeBuilder<RiskAssessmentProtectedGroup> builder)
    {
        builder.ToTable("RiskAssessmentProtectedGroup");
        builder.HasKey(x => x.Id);

        // At most one row per group per report.
        builder.HasIndex(x => new { x.TenantId, x.RiskAssessmentReportId, x.Group })
               .IsUnique();

        builder.HasIndex(x => x.RiskAssessmentReportId);
    }
}
