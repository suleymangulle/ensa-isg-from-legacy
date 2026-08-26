using Ensa.Domain.Companies;
using Ensa.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ensa.EntityFrameworkCore.Configurations.Companies;

/// <summary>
/// <see cref="OfficeExpense"/> table mapping.
/// </summary>
public class OfficeExpenseConfiguration : IEntityTypeConfiguration<OfficeExpense>
{
    public void Configure(EntityTypeBuilder<OfficeExpense> builder)
    {
        builder.ToTable("OfficeExpense");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExpenseTag)
               .IsRequired()
               .HasMaxLength(EnsaDomainSharedConsts.MaxLengths.LongName);

        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasIndex(x => x.OfficeId);

        // Periodic expense reports.
        builder.HasIndex(x => new { x.TenantId, x.ExpenseDate });
    }
}
