using Ensa.Domain.Ibys;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Ibys;

/// <summary>
/// IBYS work environment type (parent breakdown).
/// <para>A host table.</para>
/// </summary>
public class IbysWorkEnvironmentTypeConfiguration : IEntityTypeConfiguration<IbysWorkEnvironmentType>
{
    public void Configure(EntityTypeBuilder<IbysWorkEnvironmentType> builder)
    {
        builder.ToTable("IbysWorkEnvironmentType");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TypeName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.HasIndex(x => x.TypeCode).IsUnique();
    }
}
