using Ensa.Domain.Ibys;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Ibys;

/// <summary>
/// IBYS work arrangement code.
/// <para>A host table.</para>
/// </summary>
public class IbysWorkArrangementConfiguration : IEntityTypeConfiguration<IbysWorkArrangement>
{
    public void Configure(EntityTypeBuilder<IbysWorkArrangement> builder)
    {
        builder.ToTable("IbysWorkArrangement");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Type);
    }
}
