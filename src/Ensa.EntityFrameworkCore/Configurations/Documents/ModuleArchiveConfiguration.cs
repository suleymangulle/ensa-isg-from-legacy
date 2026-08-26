using Ensa.Domain.Documents;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Documents;

/// <summary>
/// <see cref="ModuleArchive"/> table mapping (per-office bulk archive header).
/// </summary>
public class ModuleArchiveConfiguration : IEntityTypeConfiguration<ModuleArchive>
{
    public void Configure(EntityTypeBuilder<ModuleArchive> builder)
    {
        builder.ToTable("ModuleArchive");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ModuleName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.ModuleCode)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // Module codes are unique within a tenant.
        builder.HasIndex(x => new { x.TenantId, x.ModuleCode }).IsUnique();
    }
}
