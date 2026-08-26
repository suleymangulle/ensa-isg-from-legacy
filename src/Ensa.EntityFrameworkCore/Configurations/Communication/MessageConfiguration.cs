using Ensa.Domain.Communication;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Communication;

/// <summary><see cref="Message"/> (in-app messaging) table mapping.</summary>
public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Message");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        // Unread message badge and inbox.
        builder.HasIndex(x => new { x.RecipientId, x.IsRead });

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.SenderId);
        builder.HasIndex(x => x.CompanyId);
    }
}
