using Ensa.Domain.Communication;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Communication;

/// <summary>
/// <see cref="Mail"/> table mapping.
/// <para>
/// <see cref="Mail.Content"/> may carry an HTML body, so it is mapped as <c>nvarchar(max)</c>.
/// </para>
/// <para>
/// The send queue index is FILTERED: it covers only the states awaiting processing
/// (Draft = 0, Queued = 1, SendFailed = 3), keeping millions of already sent rows out of the index.
/// <c>IsDeleted</c> was DELIBERATELY left out of the filter: because the soft-delete global filter can be
/// switched off at runtime, it is generated as a parameter (<c>@p = 0 OR IsDeleted = 0</c>) and SQL Server
/// would not be able to match that expression against a filtered index.
/// </para>
/// </summary>
public class MailConfiguration : IEntityTypeConfiguration<Mail>
{
    public void Configure(EntityTypeBuilder<Mail> builder)
    {
        builder.ToTable("Mail");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Sender)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Email);

        // Semicolon-separated recipient list — can be longer than a single e-mail address.
        builder.Property(x => x.Recipient)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Topic)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.Content)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        builder.Property(x => x.ErrorMessage)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        // ---------------- Indexes ----------------

        // Background send queue scan.
        builder.HasIndex(x => new { x.MailStatus, x.AttemptCount })
               .HasFilter("[MailStatus] IN (0, 1, 3)");

        // Mail history per organization.
        builder.HasIndex(x => new { x.TenantId, x.SubmissionDate });
    }
}
