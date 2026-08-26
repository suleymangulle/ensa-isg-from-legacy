using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="CompanyLedgerEntry"/> table mapping (account ledger transaction).
/// </summary>
public class CompanyLedgerEntryConfiguration : IEntityTypeConfiguration<CompanyLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CompanyLedgerEntry> builder)
    {
        builder.ToTable("CompanyLedgerEntry");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Description);

        builder.Property(x => x.Amount).HasPrecision(18, 2);

        // Account statement: company + date range.
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.Date });

        // Tracing back to the record in the source module (e.g. InvoiceId -> transaction).
        builder.HasIndex(x => new { x.SourceModule, x.OperationId });

        builder.HasIndex(x => x.CompanyId);
    }
}
