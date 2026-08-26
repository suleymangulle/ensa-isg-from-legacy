using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Past incident records of the report (work accident, near miss, occupational disease, ...).</summary>
public class RiskAssessmentHistoryRecordConfiguration : IEntityTypeConfiguration<RiskAssessmentHistoryRecord>
{
    public void Configure(EntityTypeBuilder<RiskAssessmentHistoryRecord> builder)
    {
        builder.ToTable("RiskAssessmentHistoryRecord");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.HasIndex(x => new { x.RiskAssessmentReportId, x.RecordType, x.Date });
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
