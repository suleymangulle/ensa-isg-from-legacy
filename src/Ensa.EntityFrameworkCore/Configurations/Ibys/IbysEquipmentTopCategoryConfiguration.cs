using Ensa.Domain.Ibys;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Ibys;

/// <summary>
/// IBYS work equipment top category.
/// <para>
/// A host table. IBYS does not provide a code field for this list, so uniqueness is based on the category
/// name.
/// </para>
/// </summary>
public class IbysEquipmentTopCategoryConfiguration : IEntityTypeConfiguration<IbysEquipmentTopCategory>
{
    public void Configure(EntityTypeBuilder<IbysEquipmentTopCategory> builder)
    {
        builder.ToTable("IbysEquipmentTopCategory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ParentCategoryName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => x.ParentCategoryName).IsUnique();
    }
}
