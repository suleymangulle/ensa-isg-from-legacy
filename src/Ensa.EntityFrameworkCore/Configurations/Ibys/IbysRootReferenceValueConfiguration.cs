using Ensa.Domain.Ibys;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Ibys;

/// <summary>
/// IBYS root reference value.
/// <para>A host table (it does not implement <c>IMultiTenant</c>).</para>
/// </summary>
public class IbysRootReferenceValueConfiguration : IEntityTypeConfiguration<IbysRootReferenceValue>
{
    public void Configure(EntityTypeBuilder<IbysRootReferenceValue> builder)
    {
        builder.ToTable("IbysRootReferenceValue");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.ReferenceName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
