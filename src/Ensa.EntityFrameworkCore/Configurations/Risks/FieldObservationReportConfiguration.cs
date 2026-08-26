using Ensa.Domain.Risks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>Field observation (workplace inspection round) report header.</summary>
public class FieldObservationReportConfiguration : IEntityTypeConfiguration<FieldObservationReport>
{
    public void Configure(EntityTypeBuilder<FieldObservationReport> builder)
    {
        builder.ToTable("FieldObservationReport");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.Date });
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.DepartmentId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
