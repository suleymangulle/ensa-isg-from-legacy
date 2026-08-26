using Ensa.Domain.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Communication;

/// <summary><see cref="MailAttachment"/> table mapping.</summary>
public class MailAttachmentConfiguration : IEntityTypeConfiguration<MailAttachment>
{
    public void Configure(EntityTypeBuilder<MailAttachment> builder)
    {
        builder.ToTable("MailAttachment");
        builder.HasKey(x => x.Id);

        // Reading attachments in a stable order (also served by the foreign key index).
        builder.HasIndex(x => new { x.MailId, x.OrderNo })
               .IsUnique();

        builder.HasIndex(x => x.DocumentId);
    }
}
