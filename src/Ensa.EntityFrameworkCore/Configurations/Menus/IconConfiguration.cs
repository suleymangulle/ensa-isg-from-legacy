using Ensa.Domain.Menus;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Menus;

/// <summary>
/// Icon catalogue row. A host lookup table (thousands of rows, no audit fields).
/// <para>
/// <see cref="Icon.LibraryCode"/> is not a real foreign key but a code field matching
/// <c>IconLibrary.Code</c>; it is indexed all the same, for lookups.
/// </para>
/// </summary>
public class IconConfiguration : IEntityTypeConfiguration<Icon>
{
    public void Configure(EntityTypeBuilder<Icon> builder)
    {
        builder.ToTable("Icon");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LibraryCode)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.IconCssClass)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.CssClass);

        // The same CSS class cannot appear twice in the same library.
        // Because this index leads with LibraryCode it also serves the "list by library"
        // query; no separate single-column index is created.
        builder.HasIndex(x => new { x.LibraryCode, x.IconCssClass }).IsUnique();
    }
}
