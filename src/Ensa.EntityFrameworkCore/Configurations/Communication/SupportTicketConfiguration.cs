using Ensa.Domain.Communication;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Communication;

/// <summary><see cref="SupportTicket"/> table mapping.</summary>
public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("SupportTicket");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Topic)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        // Support queue: open tickets belonging to the organization.
        builder.HasIndex(x => new { x.TenantId, x.Status });

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.OpenedByUserId);
        builder.HasIndex(x => x.ResponderUserId);
        builder.HasIndex(x => x.ClosedByUserId);
    }
}
