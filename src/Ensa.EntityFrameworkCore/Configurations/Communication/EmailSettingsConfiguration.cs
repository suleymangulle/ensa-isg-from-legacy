using Ensa.Domain.Communication;
using Ensa.Domain.Shared;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Communication;

/// <summary>
/// <see cref="EmailSettings"/> table mapping.
/// <para>
/// <see cref="EmailSettings.Password"/> is the SMTP/POP3 account password and is encrypted at column level
/// with <see cref="EncryptedStringConverter"/>.
/// </para>
/// </summary>
public class EmailSettingsConfiguration : IEntityTypeConfiguration<EmailSettings>
{
    public void Configure(EntityTypeBuilder<EmailSettings> builder)
    {
        builder.ToTable("EmailSettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Email);

        // Encrypted column — the plaintext maximum length is given, the column length is computed automatically.
        builder.Property(x => x.Password)
               .IsRequired()
               .IsEncrypted(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Pop3Server)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.SmtpServer)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        // Finding the organization's active account while sending.
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}
