using Ensa.Domain.Lookups;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Lookups;

/// <summary>
/// <see cref="TreeNode"/> table mapping.
/// <para>
/// <see cref="TreeNode.ParentTreeNodeId"/> is a self-reference; because navigation properties are banned, NO
/// foreign key relationship is configured — the column is only indexed.
/// </para>
/// <para>A host (tenant-less) reference table.</para>
/// </summary>
public class TreeNodeConfiguration : IEntityTypeConfiguration<TreeNode>
{
    public void Configure(EntityTypeBuilder<TreeNode> builder)
    {
        builder.ToTable("TreeNode");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TreeCode)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.TreeNodeCode)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.ParentTreeNodeCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.TreeNodeName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        // Item codes are unique within a tree (legacy natural key).
        builder.HasIndex(x => new { x.TreeCode, x.TreeNodeCode })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");

        // Fetching child items (self-reference).
        builder.HasIndex(x => x.ParentTreeNodeId);

        builder.HasIndex(x => x.TreeId);
    }
}
