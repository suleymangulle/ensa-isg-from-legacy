using Ensa.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="CompanyComplianceSummary"/> table mapping (denormalised summary of missing obligations).
/// </summary>
public class CompanyComplianceSummaryConfiguration : IEntityTypeConfiguration<CompanyComplianceSummary>
{
    public void Configure(EntityTypeBuilder<CompanyComplianceSummary> builder)
    {
        builder.ToTable("CompanyComplianceSummary");
        builder.HasKey(x => x.Id);

        // One summary row per company — the background job writes it with an upsert.
        // The base class carries no soft delete, so no filter is needed.
        builder.HasIndex(x => x.CompanyId).IsUnique();
    }
}
