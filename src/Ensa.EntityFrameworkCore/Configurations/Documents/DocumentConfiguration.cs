using Ensa.Domain.Documents;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Documents;

/// <summary>
/// <see cref="Document"/> table mapping — the central file store of the system.
/// <para>
/// This table carries the binary content of every module; the index design follows three access patterns:
/// (1) duplicate detection (<see cref="Document.Sha256"/>),
/// (2) polymorphic owner lookup (<see cref="Document.OwnerType"/> + <see cref="Document.OwnerRecordId"/>),
/// (3) file listing per company.
/// </para>
/// </summary>
public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Document");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.FileName);

        builder.Property(x => x.StorageName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Guid);

        builder.Property(x => x.Extension)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.ContentType)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.MimeType);

        // A SHA-256 hex representation is exactly 64 characters (the same value as MaxLengths.Guid).
        builder.Property(x => x.Sha256)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Guid);

        builder.Property(x => x.StoragePath)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Url);

        // Binary content: NO length limit — varbinary(max).
        builder.Property(x => x.Content)
               .HasColumnType("varbinary(max)");

        // ---------------- Indexes ----------------

        // Duplicate file detection (hash comparison before upload).
        builder.HasIndex(x => x.Sha256);

        // Polymorphic owner query — an index is mandatory because there is no real foreign key.
        builder.HasIndex(x => new { x.OwnerType, x.OwnerRecordId });

        // Company document list.
        builder.HasIndex(x => new { x.TenantId, x.CompanyId });

        // The storage name is GUID based; its uniqueness is guaranteed at the database level.
        builder.HasIndex(x => x.StorageName).IsUnique();

        builder.HasIndex(x => x.DocumentCategoryId);
    }
}
