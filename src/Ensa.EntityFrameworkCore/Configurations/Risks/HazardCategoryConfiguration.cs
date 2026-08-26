using Ensa.Domain.Risks;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Risks;

/// <summary>
/// Category node of the hazard library.
/// <para>
/// A MIXED (host + tenant) table; there is NO soft delete (<c>AuditedTenantEntity</c>).
/// </para>
/// </summary>
public class HazardCategoryConfiguration : IEntityTypeConfiguration<HazardCategory>
{
    public void Configure(EntityTypeBuilder<HazardCategory> builder)
    {
        builder.ToTable("HazardCategory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CategoryName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.DataType)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.ShortName);

        builder.HasIndex(x => new { x.TenantId, x.IsHazardSource, x.SortOrder });
        builder.HasIndex(x => x.TenantId);
    }
}
