using Ensa.Domain.Menus;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Menus;

/// <summary>
/// Hierarchical placement of a menu item within a given menu. A host reference table.
/// <para>
/// <see cref="MenuNode.ParentMenuNodeId"/> is a self-referencing foreign key; per the architecture no
/// relationship is configured, the column is only indexed (which also removes the risk of a cascade cycle).
/// </para>
/// </summary>
public class MenuNodeConfiguration : IEntityTypeConfiguration<MenuNode>
{
    public void Configure(EntityTypeBuilder<MenuNode> builder)
    {
        builder.ToTable("MenuNode");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Url)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Url);

        builder.Property(x => x.IconCssClass)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.CssClass);

        builder.Property(x => x.CssClass)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.CssClass);

        builder.Property(x => x.CssClass2)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.CssClass);

        builder.HasIndex(x => x.MenuItemId);
        builder.HasIndex(x => x.ParentMenuNodeId);

        // For reading the menu tree level by level.
        // No separate index for the MenuId foreign key: it is the LEADING column of this composite index.
        builder.HasIndex(x => new { x.MenuId, x.ParentMenuNodeId, x.SortOrder });
    }
}
