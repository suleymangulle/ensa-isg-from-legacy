using Ensa.Domain.Ibys;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Ibys;

/// <summary>
/// IBYS work equipment code.
/// <para>A host table.</para>
/// </summary>
public class IbysWorkEquipmentConfiguration : IEntityTypeConfiguration<IbysWorkEquipment>
{
    public void Configure(EntityTypeBuilder<IbysWorkEquipment> builder)
    {
        builder.ToTable("IbysWorkEquipment");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.ParentCategoryId);
    }
}
