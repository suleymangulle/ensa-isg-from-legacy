using Ensa.Domain.Menus;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Menus;

/// <summary>
/// Catalogue of reusable menu items. A host reference table.
/// </summary>
public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItem");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.ProjectCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Description1)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.Property(x => x.Description2)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.Property(x => x.LongDescription)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Url)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Url);

        builder.Property(x => x.QueryStringKeys)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.Property(x => x.ExtraAttributes)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.Property(x => x.IconCssClass)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.CssClass);

        builder.Property(x => x.CssClass)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.CssClass);

        builder.Property(x => x.CssClass2)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.CssClass);

        builder.Property(x => x.CssStyle)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.ModuleId);
        builder.HasIndex(x => new { x.ProjectCode, x.IsActive, x.SortOrder });
    }
}
