using Ensa.Domain.Finance;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Finance;

/// <summary><see cref="CashRegister"/> table mapping.</summary>
public class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> builder)
    {
        builder.ToTable("CashRegister");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CashRegisterName)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.Name);

        // Cash register listing per office (also served by the foreign key index).
        builder.HasIndex(x => new { x.OfficeId, x.IsActive });

        // Finding the organization's main cash register.
        builder.HasIndex(x => new { x.TenantId, x.HeadquarterCashRegister });
    }
}
