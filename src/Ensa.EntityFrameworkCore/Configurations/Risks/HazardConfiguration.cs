using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>
/// Hazard library record.
/// <para>
/// This is a MIXED (host + tenant) table: <c>TenantId = null</c> marks a shared library record available to
/// every organization, while a populated <c>TenantId</c> marks an organization-specific one. There is NO soft
/// delete (<c>AuditedTenantEntity</c>), so no index includes <c>IsDeleted</c>.
/// </para>
/// </summary>
public class HazardConfiguration : IEntityTypeConfiguration<Hazard>
{
    public void Configure(EntityTypeBuilder<Hazard> builder)
    {
        builder.ToTable("Hazard");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.HazardTag)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.RiskTag)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Measure);

        builder.Property(x => x.Likelihood).HasPrecision(9, 2);
        builder.Property(x => x.Severity).HasPrecision(9, 2);
        builder.Property(x => x.Frequency).HasPrecision(9, 2);

        builder.HasIndex(x => new { x.HazardCategoryId, x.IsActive });
        builder.HasIndex(x => x.TenantId);
    }
}
