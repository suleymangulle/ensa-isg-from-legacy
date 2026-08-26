using Ensa.Domain.Menus;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Menus;

/// <summary>
/// Icon library (Font Awesome, Metronic, ...). A host lookup table;
/// it has no audit or soft delete fields.
/// </summary>
public class IconLibraryConfiguration : IEntityTypeConfiguration<IconLibrary>
{
    public void Configure(EntityTypeBuilder<IconLibrary> builder)
    {
        builder.ToTable("IconLibrary");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}
