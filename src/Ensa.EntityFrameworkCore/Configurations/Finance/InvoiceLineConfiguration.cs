using Ensa.Domain.Finance;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Finance;

/// <summary><see cref="InvoiceLine"/> table mapping.</summary>
public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLine");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LineDescription)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.Unit)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // ---------------- Money and quantity fields ----------------

        // Quantity is not money; four decimals for fractional service quantities.
        builder.Property(x => x.Count).HasPrecision(18, 4);

        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.VatAmount).HasPrecision(18, 2);
        builder.Property(x => x.GrossWithVatAmount).HasPrecision(18, 2);

        // ---------------- Indexes ----------------

        // Reading invoice lines in a stable order (also served by the foreign key index).
        builder.HasIndex(x => new { x.InvoiceId, x.OrderNo });

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.ServiceItemId);
        builder.HasIndex(x => x.CompanyId);
    }
}
