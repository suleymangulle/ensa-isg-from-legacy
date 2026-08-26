using Ensa.Domain.Menus;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Menus;

/// <summary>
/// Free-form menu element — it carries its own presentation data on its row. A host reference table.
/// <para>
/// <see cref="MenuElement.ParentMenuElementId"/> is a self-referencing foreign key; no relationship is
/// configured, the column is only indexed.
/// </para>
/// </summary>
public class MenuElementConfiguration : IEntityTypeConfiguration<MenuElement>
{
    public void Configure(EntityTypeBuilder<MenuElement> builder)
    {
        builder.ToTable("MenuElement");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.IconCssClass)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.CssClass);

        builder.Property(x => x.CssClass)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.CssClass);

        builder.Property(x => x.CssStyle)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.Property(x => x.Url)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Url);

        builder.Property(x => x.UrlParameters)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.HasIndex(x => x.ParentMenuElementId);

        // No separate index for the MenuId foreign key: it is the LEADING column of this composite index.
        builder.HasIndex(x => new { x.MenuId, x.ParentMenuElementId, x.SortOrder });
    }
}
