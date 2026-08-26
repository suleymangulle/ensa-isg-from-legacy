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

        // Invoice numbers are unique within a tenant; deleted rows are out of scope.
        builder.HasIndex(x => new { x.TenantId, x.InvoiceNo })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");

        // Account statement / periodic invoice lists.
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.InvoiceDate });

        // Periodic turnover reports.
        builder.HasIndex(x => new { x.TenantId, x.InvoiceDate });

        // Foreign key index (no relationship is configured — index only).
        builder.HasIndex(x => x.OfficeId);
    }
}
