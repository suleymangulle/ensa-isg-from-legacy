using Ensa.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Finance;

/// <summary><see cref="Payment"/> table mapping.</summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payment");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 2);

        // Listing payment notifications awaiting approval.
        builder.HasIndex(x => new { x.TenantId, x.Status, x.NotificationDate });

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.BankId);
        builder.HasIndex(x => x.ReceiptDocumentId);
    }
}
