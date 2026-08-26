using Ensa.Domain.Shared;
using Ensa.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Tenancy;

/// <summary>
/// Field visibility configuration of the sales representative screens. A host table; it does not implement soft delete.
/// </summary>
public class SalesRepScreenFieldConfiguration : IEntityTypeConfiguration<SalesRepScreenField>
{
    public void Configure(EntityTypeBuilder<SalesRepScreenField> builder)
    {
        builder.ToTable("SalesRepScreenField");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.DisplayedName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // A field can be configured only once per screen.
        builder.HasIndex(x => new { x.ScreenType, x.FieldName }).IsUnique();
        builder.HasIndex(x => new { x.ScreenType, x.SortOrder });
    }
}
