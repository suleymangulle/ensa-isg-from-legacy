using Ensa.Domain.Ibys;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Ibys;

/// <summary>
/// IBYS work environment code.
/// <para>A host table.</para>
/// </summary>
public class IbysWorkEnvironmentConfiguration : IEntityTypeConfiguration<IbysWorkEnvironment>
{
    public void Configure(EntityTypeBuilder<IbysWorkEnvironment> builder)
    {
        builder.ToTable("IbysWorkEnvironment");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Environment)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.HasIndex(x => x.EnvironmentCode).IsUnique();
        builder.HasIndex(x => x.TypeCode);
        builder.HasIndex(x => x.IbysWorkEnvironmentTypeId);
    }
}
