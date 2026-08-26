using Ensa.Domain.Finance;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Finance;

/// <summary><see cref="CashTransaction"/> table mapping.</summary>
public class CashTransactionConfiguration : IEntityTypeConfiguration<CashTransaction>
{
    public void Configure(EntityTypeBuilder<CashTransaction> builder)
    {
        builder.ToTable("CashTransaction");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Text);

        builder.Property(x => x.OperationAmount).HasPrecision(18, 2);

        // ---------------- Indexes ----------------

        // Cash register statement: date-ordered transaction listing per register.
        builder.HasIndex(x => new { x.CashRegisterId, x.OperationDate });

        // Finding the transaction from its source record (e.g. the collection for an invoice).
        builder.HasIndex(x => new { x.SourceModule, x.SourceRecordId });

        // Foreign key indexes (no relationship is configured — index only).
        builder.HasIndex(x => x.ExitItemId);
        builder.HasIndex(x => x.PaymentMethodId);
    }
}
