using Ensa.Domain.Finance;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Finance;

/// <summary><see cref="Bank"/> table mapping.</summary>
public class BankConfiguration : IEntityTypeConfiguration<Bank>
{
    public void Configure(EntityTypeBuilder<Bank> builder)
    {
        builder.ToTable("Bank");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BankName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.Iban)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Iban);

        builder.Property(x => x.Recipient)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.BranchName)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        builder.Property(x => x.AccountNo)
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Code);

        // Listing the organization's active collection accounts on the payment screen.
        builder.HasIndex(x => new { x.TenantId, x.IsActive });

        // Foreign key index (no relationship is configured — index only).
        builder.HasIndex(x => x.ImageDocumentId);
    }
}
