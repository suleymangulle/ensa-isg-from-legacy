using Ensa.Domain.Communication;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Communication;

/// <summary><see cref="SupportTicketMessage"/> table mapping.</summary>
public class SupportTicketMessageConfiguration : IEntityTypeConfiguration<SupportTicketMessage>
{
    public void Configure(EntityTypeBuilder<SupportTicketMessage> builder)
    {
        builder.ToTable("SupportTicketMessage");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Note);

        // The correspondence listing of a ticket.
        builder.HasIndex(x => x.SupportTicketId);

        // Unread support message badge.
        builder.HasIndex(x => new { x.FieldUserId, x.IsRead });

        builder.HasIndex(x => x.SenderUserId);
    }
}
