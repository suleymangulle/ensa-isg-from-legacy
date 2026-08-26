using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="Tree"/> table mapping (root of a hierarchical code list).
/// <para>A host (tenant-less) reference table.</para>
/// </summary>
public class TreeConfiguration : IEntityTypeConfiguration<Tree>
{
    public void Configure(EntityTypeBuilder<Tree> builder)
    {
        builder.ToTable("Tree");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TreeCode)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.TreeName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => x.TreeCode).IsUnique();
    }
}
