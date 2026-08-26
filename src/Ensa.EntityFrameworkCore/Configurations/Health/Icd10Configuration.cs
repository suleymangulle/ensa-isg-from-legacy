using Ensa.Domain.Health;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Health;

/// <summary>
/// SKRS ICD-10 diagnosis code reference.
/// <para>A host table (it does not implement <c>IMultiTenant</c>) — there is no <c>TenantId</c> index.</para>
/// </summary>
public class Icd10Configuration : IEntityTypeConfiguration<Icd10>
{
    public void Configure(EntityTypeBuilder<Icd10> builder)
    {
        builder.ToTable("Icd10");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.ParentCode)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.ParentCode);
        builder.HasIndex(x => x.ParentIcd10Id);
    }
}
