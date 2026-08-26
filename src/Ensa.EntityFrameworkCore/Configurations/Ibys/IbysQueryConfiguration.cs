using Ensa.Domain.Ibys;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Ibys;

/// <summary>
/// A notification/query record submitted to IBYS.
/// <para>
/// <b>XmlData / SignedData.</b> These payloads have no length limit, so they are configured as
/// <c>nvarchar(max)</c>. Because they carry personal health data they are encrypted as well; the
/// <c>IsEncrypted(int)</c> extension is NOT used here, since it requires a plaintext length and applies
/// <c>HasMaxLength</c> — the converter is attached directly with <c>HasConversion</c> and the column type is
/// set by hand.
/// </para>
/// </summary>
public class IbysQueryConfiguration : IEntityTypeConfiguration<IbysQuery>
{
    public void Configure(EntityTypeBuilder<IbysQuery> builder)
    {
        builder.ToTable("IbysQuery");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QueryNo)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.IbysMessage)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.GroupId)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Guid);

        builder.Property(x => x.IbysVersion)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // Signature timestamp token (base64) — a single long line of text.
        builder.Property(x => x.TimeStamp)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        builder.Property(x => x.XmlData!)
               .HasConversion(new EncryptedStringConverter())
               .HasColumnType("nvarchar(max)");

        builder.Property(x => x.SignedData!)
               .HasConversion(new EncryptedStringConverter())
               .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.TenantId, x.QueryType, x.Status });
        builder.HasIndex(x => x.QueryNo);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.CompanyEmployeeId);
        builder.HasIndex(x => x.GroupId);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });
    }
}
