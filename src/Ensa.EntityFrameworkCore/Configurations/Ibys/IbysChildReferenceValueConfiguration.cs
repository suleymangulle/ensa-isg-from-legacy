using Ensa.Domain.Ibys;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Ibys;

/// <summary>
/// IBYS child (dependent) reference value.
/// <para>A host table. The same code may repeat under different parent references.</para>
/// </summary>
public class IbysChildReferenceValueConfiguration : IEntityTypeConfiguration<IbysChildReferenceValue>
{
    public void Configure(EntityTypeBuilder<IbysChildReferenceValue> builder)
    {
        builder.ToTable("IbysChildReferenceValue");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.ReferenceName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.ParentReferenceCode)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.HasIndex(x => new { x.Code, x.ParentReferenceCode }).IsUnique();
        builder.HasIndex(x => x.ParentReferenceCode);
        builder.HasIndex(x => x.IbysRootReferenceValueId);
    }
}
