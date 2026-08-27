using Ensa.Domain.Finance;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Finance;

/// <summary><see cref="Invoice"/> table mapping.</summary>
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoice");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceNo)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        builder.Property(x => x.AccountCurrentName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.InvoiceDescription)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.InWords)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        // ---------------- Money fields ----------------

        builder.Property(x => x.Total).HasPrecision(18, 2);
        builder.Property(x => x.VatTotal).HasPrecision(18, 2);
        builder.Property(x => x.GeneralTotal).HasPrecision(18, 2);

        // ---------------- Indexes ----------------

        // Invoice number lookup within a tenant. NOT unique, and that is a finding rather than an
        // omission: the migrated data has 4,375 invoices with no number at all, and 3,816 more
        // that repeat within their organization - one organization writes the literal text
        // "EARSIV" on 1,689 invoices to 190 different companies. Only 16 of the repeats share a
        // company, a date and a total, so these are different invoices that happen to carry the
        // same text, not duplicates. A unique index here would have forced the migration to
        // fabricate fiscal document numbers or to discard eight thousand invoices.
        builder.HasIndex(x => new { x.TenantId, x.InvoiceNo });

        // Account statement / periodic invoice lists.
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.InvoiceDate });

        // Periodic turnover reports.
        builder.HasIndex(x => new { x.TenantId, x.InvoiceDate });

        // Foreign key index (no relationship is configured — index only).
        builder.HasIndex(x => x.OfficeId);
    }
}
