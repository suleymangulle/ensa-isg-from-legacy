using Ensa.Domain.Menus;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Menus;

/// <summary>
/// Menu layout type (side menu, top menu, quick access). A host reference table.
/// </summary>
public class MenuTypeConfiguration : IEntityTypeConfiguration<MenuType>
{
    public void Configure(EntityTypeBuilder<MenuType> builder)
    {
        builder.ToTable("MenuType");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.ProjectCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.ProjectCode, x.SortOrder });
    }
}
