using Ensa.Domain.Documents;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Documents;

/// <summary>
/// <see cref="StandardDocument"/> table mapping (standard document type definition).
/// <para>A host (tenant-less) reference table.</para>
/// </summary>
public class StandardDocumentConfiguration : IEntityTypeConfiguration<StandardDocument>
{
    public void Configure(EntityTypeBuilder<StandardDocument> builder)
    {
        builder.ToTable("StandardDocument");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StandardDocumentName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.StandardDocumentCode)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.HasIndex(x => x.StandardDocumentCode).IsUnique();
    }
}
